
public enum VehicleEngine{  MainIgnite,  MainShutdown,}

public enum FlightComputerAction{  [XmlEnum("None")] None,  [XmlEnum("DeleteNextBurn")] DeleteNextBurn,  [XmlEnum("WarpToNextBurn")] WarpToNextBurn,}

public enum FlightComputerAttitudeProfile{  Strict,  Balanced,  Relaxed,}

public enum FlightComputerAttitudeTrackTarget{  None,  Custom,  Forward,  Backward,  Up,  Down,  Ahead,  Behind,  RadialOut,  RadialIn,  Prograde,  Retrograde,  Normal,  AntiNormal,  Outward,  Inward,  PositiveDv,  NegativeDv,  Toward,  Away,  Antivel,  Align,}

public enum VehicleReferenceFrame{  [XmlEnum("EclBody")] EclBody,  [XmlEnum("EnuBody")] EnuBody,  [XmlEnum("Lvlh")] Lvlh,  [XmlEnum("VlfBody")] VlfBody,  [XmlEnum("BurnBody")] BurnBody,  [XmlEnum("Tgt")] Dock,}

public enum FlightComputerBurnMode{  Manual,  Auto,}

public enum FlightComputerRollMode{  Decoupled,  Up,  Down,}

public enum FlightComputerAttitudeMode{  Manual,  Auto,}

public enum FlightComputerManualThrustMode{  Direct,  Pulse,}
