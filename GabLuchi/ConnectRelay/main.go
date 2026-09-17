package main

import (
	"crypto/rand"
	"encoding/hex"
	"encoding/json"
	"log"
	"net"
	"net/http"
	"os"
	"sync"
	"time"

	"github.com/gorilla/websocket"
)

var upgrader = websocket.Upgrader{
	CheckOrigin: func(r *http.Request) bool { return true },
}

type LobbyInfo struct {
	GameName string `json:"gameName"`
	AppID    int64  `json:"appId"`
	HostName string `json:"hostName"`
	IP       string `json:"ip"`
	Port     int    `json:"port"`
}

type LobbyEntry struct {
	Code      string    `json:"code"`
	GameName  string    `json:"gameName"`
	AppID     int64     `json:"appId"`
	HostName  string    `json:"hostName"`
	IP        string    `json:"ip"`
	Port      int       `json:"port"`
	CreatedAt time.Time `json:"createdAt"`
}

type PendingLobby struct {
	Info      LobbyInfo
	CreatedAt time.Time
	Conn      *websocket.Conn
}

var (
	lobbies   = make(map[string]*PendingLobby)
	lobbiesMu sync.Mutex
)

const (
	codeLength   = 6
	lobbyTTL     = 5 * time.Minute
	cleanupEvery = 30 * time.Second
	maxConns     = 10
)

type RateLimiter struct {
	mu       sync.Mutex
	requests map[string][]time.Time
	limit    int
	window   time.Duration
}

func NewRateLimiter(limit int, window time.Duration) *RateLimiter {
	return &RateLimiter{
		requests: make(map[string][]time.Time),
		limit:    limit,
		window:   window,
	}
}

func (rl *RateLimiter) Allow(key string) bool {
	rl.mu.Lock()
	defer rl.mu.Unlock()

	now := time.Now()
	cutoff := now.Add(-rl.window)

	reqs := rl.requests[key]
	filtered := make([]time.Time, 0, len(reqs))
	for _, t := range reqs {
		if t.After(cutoff) {
			filtered = append(filtered, t)
		}
	}

	if len(filtered) >= rl.limit {
		rl.requests[key] = filtered
		return false
	}

	rl.requests[key] = append(filtered, now)
	return true
}

var limiter = NewRateLimiter(maxConns, time.Minute)

func generateCode() string {
	b := make([]byte, 3)
	rand.Read(b)
	return hex.EncodeToString(b)
}

func cleanup() {
	ticker := time.NewTicker(cleanupEvery)
	defer ticker.Stop()
	for range ticker.C {
		lobbiesMu.Lock()
		now := time.Now()
		for code, lobby := range lobbies {
			if now.Sub(lobby.CreatedAt) > lobbyTTL {
				log.Printf("cleanup: removing expired lobby %s (%s)", code, lobby.Info.GameName)
				delete(lobbies, code)
			}
		}
		lobbiesMu.Unlock()
	}
}

type HostRequest struct {
	Action string `json:"action"`
	LobbyInfo
}

type JoinRequest struct {
	Action string `json:"action"`
	Code   string `json:"code"`
}

type Response struct {
	OK      bool       `json:"ok"`
	Code    string     `json:"code,omitempty"`
	Lobby   *LobbyInfo `json:"lobby,omitempty"`
	Error   string     `json:"error,omitempty"`
}

func handleWS(w http.ResponseWriter, r *http.Request) {
	ip, _, _ := net.SplitHostPort(r.RemoteAddr)
	if !limiter.Allow(ip) {
		http.Error(w, "rate limited", http.StatusTooManyRequests)
		return
	}

	conn, err := upgrader.Upgrade(w, r, nil)
	if err != nil {
		log.Printf("upgrade: %v", err)
		return
	}
	defer conn.Close()

	conn.SetReadDeadline(time.Now().Add(30 * time.Second))

	_, raw, err := conn.ReadMessage()
	if err != nil {
		return
	}

	var hostReq HostRequest
	if err := json.Unmarshal(raw, &hostReq); err != nil {
		writeJSON(conn, Response{OK: false, Error: "invalid json"})
		return
	}

	switch hostReq.Action {
	case "host":
		handleHost(conn, hostReq)
	case "join":
		var joinReq JoinRequest
		if err := json.Unmarshal(raw, &joinReq); err != nil {
			writeJSON(conn, Response{OK: false, Error: "invalid json"})
			return
		}
		handleJoin(conn, joinReq)
	default:
		writeJSON(conn, Response{OK: false, Error: "unknown action"})
	}
}

func handleHost(conn *websocket.Conn, req HostRequest) {
	code := generateCode()

	hostName := req.HostName
	if hostName == "" {
		hostName = "Host"
	}

	lobby := &PendingLobby{
		Info: LobbyInfo{
			GameName: req.GameName,
			AppID:    req.AppID,
			HostName: hostName,
			IP:       req.IP,
			Port:     req.Port,
		},
		CreatedAt: time.Now(),
		Conn:      conn,
	}

	lobbiesMu.Lock()
	lobbies[code] = lobby
	lobbiesMu.Unlock()

	log.Printf("host: code=%s game=%s host=%s app=%d", code, req.GameName, hostName, req.AppID)

	writeJSON(conn, Response{OK: true, Code: code})

	// clear the read deadline from handleWS so the keepalive loop doesn't time out
	conn.SetReadDeadline(time.Time{})

	// keep connection alive — lobby alive while host is connected
	// read until disconnect (ignore messages, just detect close)
	for {
		_, _, err := conn.ReadMessage()
		if err != nil {
			break
		}
	}
	}

	// host disconnected — remove lobby
	lobbiesMu.Lock()
	if current, exists := lobbies[code]; exists && current.Conn == conn {
		delete(lobbies, code)
		log.Printf("host disconnected: code=%s game=%s", code, req.GameName)
	}
	lobbiesMu.Unlock()
}

func handleJoin(conn *websocket.Conn, req JoinRequest) {
	lobbiesMu.Lock()
	lobby, exists := lobbies[req.Code]
	if exists {
		delete(lobbies, req.Code)
	}
	lobbiesMu.Unlock()

	if !exists {
		writeJSON(conn, Response{OK: false, Error: "invalid or expired code"})
		return
	}

	log.Printf("join: code=%s game=%s", req.Code, lobby.Info.GameName)

	writeJSON(conn, Response{OK: true, Lobby: &lobby.Info})
}

func handleLobbies(w http.ResponseWriter, r *http.Request) {
	w.Header().Set("Content-Type", "application/json")
	w.Header().Set("Access-Control-Allow-Origin", "*")

	lobbiesMu.Lock()
	entries := make([]LobbyEntry, 0, len(lobbies))
	for code, lobby := range lobbies {
		entries = append(entries, LobbyEntry{
			Code:      code,
			GameName:  lobby.Info.GameName,
			AppID:     lobby.Info.AppID,
			HostName:  lobby.Info.HostName,
			IP:        lobby.Info.IP,
			Port:      lobby.Info.Port,
			CreatedAt: lobby.CreatedAt,
		})
	}
	lobbiesMu.Unlock()

	json.NewEncoder(w).Encode(entries)
}

func writeJSON(conn *websocket.Conn, v interface{}) {
	conn.SetWriteDeadline(time.Now().Add(5 * time.Second))
	conn.WriteJSON(v)
}

func healthHandler(w http.ResponseWriter, r *http.Request) {
	w.WriteHeader(http.StatusOK)
	w.Write([]byte("ok"))
}

func startKeepAlive(selfURL string) {
	if selfURL == "" {
		return
	}
	go func() {
		ticker := time.NewTicker(5 * time.Minute)
		defer ticker.Stop()
		client := &http.Client{Timeout: 10 * time.Second}
		for range ticker.C {
			resp, err := client.Get(selfURL + "/health")
			if err == nil {
				resp.Body.Close()
				log.Printf("keepalive: ping ok")
			} else {
				log.Printf("keepalive: ping failed: %v", err)
			}
		}
	}()
}

func main() {
	port := os.Getenv("PORT")
	if port == "" {
		port = "8080"
	}

	selfURL := os.Getenv("RENDER_EXTERNAL_URL")
	if selfURL == "" {
		selfURL = os.Getenv("SELF_URL")
	}

	go cleanup()
	go startKeepAlive(selfURL)

	http.HandleFunc("/ws", handleWS)
	http.HandleFunc("/lobbies", handleLobbies)
	http.HandleFunc("/health", healthHandler)

	log.Printf("gabluchi-connect relay starting on :%s (selfURL=%s)", port, selfURL)
	if err := http.ListenAndServe(":"+port, nil); err != nil {
		log.Fatal(err)
	}
}
