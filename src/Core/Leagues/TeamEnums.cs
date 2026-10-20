namespace GridironFrontOffice.Core.Leagues;

/// <summary>The crest's outline (F-02 §4.4).</summary>
public enum CrestShape { Shield, Roundel, Hexagon, Diamond, Banner }

/// <summary>The crest's stripe (F-02 §4.4). "Single" and "Double" clash with .NET type names (CA1720).</summary>
public enum CrestStripe { None, Bar, TwinBars, Chevron, Sash }

/// <summary>Stadium flavor (F-02 §4.2). No game effect in 1.0.</summary>
public enum Roof { Open, Dome, Retractable }

public enum Surface { Grass, Turf }

public enum ClimateZone { Cold, Temperate, Hot }
