using System.IO;

namespace Fmod5Sharp.ChunkData
{
	public  interface IChunkData
	{
		public void Read(BinaryReader reader, uint expectedSize);
	}
}