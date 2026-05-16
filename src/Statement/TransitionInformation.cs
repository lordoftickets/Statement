using System;

namespace Statement;

public record struct TransitionInformation(object? From, object? To, Type?   FromType, Type?   ToType);