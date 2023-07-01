using NBrigadier;
using NBrigadier.Exceptions;

using StringReader = NBrigadier.StringReader;

namespace Cgsrl.Server.Arguments;

public class CoordinateArgument {
    private static readonly SimpleCommandExceptionType missingCoordinate =
        new(new LiteralMessage("argument.pos.missing.int"));

    public bool relative { get; }
    private readonly int _value;

    private CoordinateArgument(bool relative, int value) {
        this.relative = relative;
        _value = value;
    }

    public int ToAbsoluteCoordinate(int offset) => relative ? _value + offset : _value;

    public static CoordinateArgument Parse(StringReader reader) {
        if(!reader.CanRead() || reader.Peek() == '^')
            throw missingCoordinate.CreateWithContext(reader);
        bool isRelative = IsRelative(reader);
        int coord;
        if(reader.CanRead() && reader.Peek() != ' ')
            coord = reader.ReadInt();
        else
            coord = 0;
        return new CoordinateArgument(isRelative, coord);
    }

    private static bool IsRelative(StringReader reader) {
        if(reader.Peek() != '~')
            return false;
        reader.Skip();
        return true;
    }
}
