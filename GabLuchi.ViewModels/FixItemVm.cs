using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using GabLuchi.Models;

namespace GabLuchi.ViewModels;

public class FixItemVm(DenuvoFix f) : ObservableObject
{
	public string Id { get; } = f.Id;

	public string Title { get; } = f.Title;

	public string? Description { get; } = f.Description;

	public IReadOnlyList<DenuvoTag> Tags { get; } = f.Tags;

	public bool HasManifest { get; } = f.HasManifest;

	public bool HasFix { get; } = f.HasFix;

	public string? ManifestFilename { get; } = f.ManifestFilename;

	public string? FixFilename { get; } = f.FixFilename;

	public string DateLabel { get; } = FormatDate(f.CreatedAt);

	private static string FormatDate(string? iso)
	{
		if (!DateTimeOffset.TryParse(iso, out var result))
		{
			return "";
		}
		return result.UtcDateTime.ToString("d MMM yyyy");
	}
}
