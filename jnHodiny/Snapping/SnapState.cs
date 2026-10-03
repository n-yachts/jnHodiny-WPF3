namespace jnHodiny.Snapping
{
    /// <summary>Ke kterým okrajům pracovní plochy je okno právě přichycené.</summary>
    public readonly record struct SnapState(bool Left, bool Right, bool Top, bool Bottom)
    {
        public static SnapState None => default;
    }
}
