﻿namespace SF_Server;

public static class Extensions
{
    public static string Truncate(this string value, int maxChars)
    { // Credit to: https://stackoverflow.com/a/6724896  
        return value.Length <= maxChars ? value : value[..maxChars] + "...";
    }
}