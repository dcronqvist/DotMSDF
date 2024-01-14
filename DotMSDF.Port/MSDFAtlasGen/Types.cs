namespace DotMSDF.Port;

public enum ImageType
{
    HARD_MASK,
    SOFT_MASK,
    SDF,
    PSDF,
    MSDF,
    MTSDF
}

public enum ImageFormat
{
    UNSPECIFIED,
    PNG,
    BMP,
    TIFF,
    TEXT,
    TEXT_FLOAT,
    BINARY,
    BINARY_FLOAT,
    BINARY_FLOAT_BE
}

public enum GlyphIdentifierType
{
    GLYPH_INDEX,
    UNICODE_CODEPOINT
}

public enum YDirection
{
    BOTTOM_UP,
    TOP_DOWN
}