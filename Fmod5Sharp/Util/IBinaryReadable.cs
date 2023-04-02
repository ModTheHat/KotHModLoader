using System.IO;

namespace Fmod5Sharp.Util
{
    public interface IBinaryReadable
    {
        internal void Read(BinaryReader reader);
    }
}