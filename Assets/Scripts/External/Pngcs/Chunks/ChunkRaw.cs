namespace Pngcs.Chunks
{
	
	/// <summary> Wraps the raw chunk data </summary>
	/// <remarks>
	/// Short lived object, to be created while serialing/deserializing 
	/// 
	/// Do not reuse it for different chunks
	/// 
	/// See http://www.libpng.org/pub/png/spec/1.2/PNG-Chunks.html
	///</remarks>
	public class ChunkRaw
	{

		/// <summary>
		/// The length counts only the data field, not itself, the chunk type code, or the CRC. Zero is a valid length.
		/// Although encoders and decoders should treat the length as unsigned, its value must not exceed 2^31-1 bytes.
		/// </summary>
		public readonly int Len;

		/// <summary> Chunk Id, as array of 4 bytes </summary>
		public readonly byte[] IdBytes;

		public readonly string Id;

		/// <summary> Raw data, crc not included </summary>
		public byte[] Data;

		int crcval;
		public long offset = 0;

		/// <summary> Creates an empty raw chunk </summary>
		internal ChunkRaw ( int length , string idb , bool alloc )
		{
			this.Id = idb;
			this.IdBytes = ChunkHelper.ToBytes(Id);
			this.Data = null;
			this.crcval = 0;
			this.Len = length;
			if( alloc ) AllocData();
		}

		internal ChunkRaw ( int length , byte[] idbytes , bool alloc )
			: this( length , ChunkHelper.ToString(idbytes) , alloc )
		{

		}

		/// <summary> Called after setting data, before writing to os </summary>
		int ComputeCrc ()
		{
			Zlib.CRC32 crcengine = PngHelperInternal.GetCRC();
			crcengine.Reset();
			crcengine.Update( IdBytes , 0 , 4 );
			if( Len>0 )
			{
				crcengine.Update( Data , 0 , Len );
			}
			return (int)crcengine.GetValue();
		}


		internal void WriteChunk ( System.IO.Stream os )
		{
			if( IdBytes.Length!=4 ) throw new System.IO.IOException($"bad chunkid [{Chunks.ChunkHelper.ToString(IdBytes)}]");
			crcval = ComputeCrc();
			PngHelperInternal.WriteInt4(os, Len);
			PngHelperInternal.WriteBytes(os, IdBytes);
			if( Len>0 )
				PngHelperInternal.WriteBytes(os, Data, 0, Len);
			//Console.WriteLine("writing chunk " + this.ToString() + "crc=" + crcval);
			PngHelperInternal.WriteInt4(os, crcval);
		}

		/// <summary>
		/// Position before: just after chunk id. positon after: after crc Data should
		/// be already allocated. Checks CRC Return number of byte read.
		/// </summary>
		internal int ReadChunkData ( System.IO.Stream stream , bool checkCrc )
		{
			PngHelperInternal.ReadBytes( stream , Data , 0 , Len );
			crcval = PngHelperInternal.ReadInt4( stream );
			if( checkCrc )
			{
				int crc = ComputeCrc();
				if( crc!=crcval ) throw new System.Exception($"crc invalid for chunk {this} calc={crc} read={crcval}");
			}
			return Len + 4;
		}

		// only the data
		internal System.IO.MemoryStream GetAsByteStream () => new System.IO.MemoryStream(Data);

		void AllocData ()
		{
			if( Data==null || Data.Length<Len )
			{
				Data = new byte[Len];
			}
		}

		/// <summary> Just id and length </summary>
		public override string ToString () => $"chunkid={Chunks.ChunkHelper.ToString(IdBytes)} len={Len}";

	}
}
