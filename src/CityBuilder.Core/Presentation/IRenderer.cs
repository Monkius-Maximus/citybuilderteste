using CityBuilder.Grid;

namespace CityBuilder.Presentation;

/// <summary>
/// The drawing contract the presentation layer implements (Unity, Godot, or a console/ASCII
/// view). It is defined in Core only as an interface: the SIMULATION NEVER CALLS IT. Instead
/// the presentation observes the event bus and reads world state each frame, then issues these
/// draw calls. This is the hard boundary that keeps the simulation engine-agnostic and headless-capable.
/// </summary>
public interface IRenderer
{
    void BeginFrame();

    /// <summary>
    /// Draw one placeholder primitive at a projected screen position. <paramref name="depthKey"/>
    /// comes from <see cref="IsometricProjector.DepthKey"/> for correct back-to-front ordering.
    /// </summary>
    void DrawIso(ScreenPoint center, in TileVisual visual, long depthKey);

    void EndFrame();
}
