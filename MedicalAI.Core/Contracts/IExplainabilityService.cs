using MedicalAI.Core.Models;

namespace MedicalAI.Core.Contracts;

public interface IExplainabilityService
{
	/// <summary>
	/// Generates a Grad-CAM overlay for the requested class using encoded image bytes.
	/// </summary>
	Task<HeatmapOverlay> GenerateGradCamAsync(
		byte[] imageBytes,
		string targetClass,
		CancellationToken cancellationToken = default);
}
