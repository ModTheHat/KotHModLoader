using System.IO;

namespace Fmod5Sharp.ChunkData
{
	public class FrequencyChunkData : IChunkData
	{
		public uint ActualFrequencyId;

		public void Read(BinaryReader reader, uint expectedSize)
		{
			ActualFrequencyId = reader.ReadUInt32();
		}
	}
}