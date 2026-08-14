using System;
using System.Net;

namespace GabLuchi.Services;

public class ApiException(string message, HttpStatusCode? status = null) : Exception(message)
{
	public HttpStatusCode? Status { get; } = status;
}
