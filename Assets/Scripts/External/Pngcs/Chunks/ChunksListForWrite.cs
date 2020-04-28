using IO = System.IO;
using System.Collections.Generic;

namespace Pngcs.Chunks
{
	/// <summary> Chunks written or queued to be written http://www.w3.org/TR/PNG/#table53 </summary>
	public class ChunksListForWrite : ChunksList
	{

		List<PngChunk> queuedChunks; // chunks not yet writen - does not include IHDR, IDAT, END, perhaps yes PLTE

		// redundant, just for eficciency
		Dictionary<string,int> alreadyWrittenKeys;

		internal ChunksListForWrite ( ImageInfo info )
			: base(info)
		{
			this.queuedChunks = new List<PngChunk>();
			this.alreadyWrittenKeys = new Dictionary<string,int>();
		}

		/// <summary> Same as <c>getById()</c>, but looking in the queued chunks </summary>
		public List<PngChunk> GetQueuedById ( string id ) => GetQueuedById( id , null );

		/// <summary> Same as <c>getById()</c>, but looking in the queued chunks </summary>
		public List<PngChunk> GetQueuedById ( string id , string innerid ) => GetXById( queuedChunks , id , innerid );

		/// <summary> Same as <c>getById()</c>, but looking in the queued chunks </summary>
		public PngChunk GetQueuedById1 ( string id , string innerid , bool failIfMultiple )
		{
			List<PngChunk> list = GetQueuedById( id , innerid );
			if( list.Count==0 ) return null;
			if( list.Count>1 && (failIfMultiple || !list[0].AllowsMultiple()) ) throw new System.Exception($"unexpected multiple chunks id={id}" );
			return list[list.Count-1];
		}

		/// <summary> Same as <c>getById1()</c>, but looking in the queued chunks </summary>
		public PngChunk GetQueuedById1 ( string id , bool failIfMultiple ) => GetQueuedById1( id , null , failIfMultiple );

		/// <summary> Same as getById1(), but looking in the queued chunks </summary>
		public PngChunk GetQueuedById1 ( string id ) => GetQueuedById1( id , false );

		/// <summary> Remove Chunk: only from queued  </summary>
		/// <remarks>
		/// WARNING: this depends on chunk.Equals() implementation, which is straightforward for SingleChunks. For 
		/// MultipleChunks, it will normally check for reference equality!
		/// </remarks>
		public bool RemoveChunk ( PngChunk chunk ) => queuedChunks.Remove(chunk);

		/// <summary> Adds chunk to queue </summary>
		/// <remarks> Does not check for duplicated or anything </remarks>
		public bool Queue ( PngChunk chunk )
		{
			queuedChunks.Add(chunk);
			return true;
		}

		/**
		 * this should be called only for ancillary chunks and PLTE (groups 1 - 3 - 5)
		 **/
		static bool ShouldWrite ( PngChunk chunk , int currentGroup )
		{
			if( currentGroup==CHUNK_GROUP_2_PLTE ) return chunk.Id.Equals(ChunkHelper.PLTE);
			if( currentGroup%2==0 ) throw new System.IO.IOException("bad chunk group?");
			int minChunkGroup, maxChunkGroup;
			if( chunk.MustGoBeforePLTE() )
				minChunkGroup = maxChunkGroup = ChunksList.CHUNK_GROUP_1_AFTERIDHR;
			else if( chunk.MustGoBeforeIDAT() )
			{
				maxChunkGroup = ChunksList.CHUNK_GROUP_3_AFTERPLTE;
				minChunkGroup = chunk.MustGoAfterPLTE()
					? ChunksList.CHUNK_GROUP_3_AFTERPLTE
					: ChunksList.CHUNK_GROUP_1_AFTERIDHR;
			}
			else
			{
				maxChunkGroup = ChunksList.CHUNK_GROUP_5_AFTERIDAT;
				minChunkGroup = ChunksList.CHUNK_GROUP_1_AFTERIDHR;
			}

			int preferred = maxChunkGroup;
			if( chunk.Priority )
				preferred = minChunkGroup;
			if( ChunkHelper.IsUnknown(chunk) && chunk.ChunkGroup>0 )
				preferred = chunk.ChunkGroup;
			
			if( currentGroup==preferred ) return true;
			if( currentGroup>preferred && currentGroup<=maxChunkGroup ) return true;
			return false;
		}

		internal int WriteChunks ( IO.Stream os , int currentGroup )
		{
			List<int> written = new List<int>();
			for( int i=0 ; i<queuedChunks.Count ; i++ )
			{
				PngChunk chunk = queuedChunks[i];

				if( !ShouldWrite(chunk,currentGroup) ) continue;
				
				if( ChunkHelper.IsCritical(chunk.Id) && !chunk.Id.Equals(ChunkHelper.PLTE) ) throw new System.IO.IOException($"bad chunk queued: {chunk}");
				if( alreadyWrittenKeys.ContainsKey(chunk.Id) && !chunk.AllowsMultiple() ) throw new System.IO.IOException($"duplicated chunk does not allow multiple: {chunk}");
				
				chunk.Write(os);
				chunks.Add(chunk);
				alreadyWrittenKeys[chunk.Id] = alreadyWrittenKeys.ContainsKey(chunk.Id) ? alreadyWrittenKeys[chunk.Id] + 1 : 1;
				written.Add(i);
				chunk.ChunkGroup = currentGroup;
			}
			for( int k=written.Count-1 ; k!=-1 ; k-- )
			{
				queuedChunks.RemoveAt( written[k] );
			}
			return written.Count;
		}

		/// <summary> chunks not yet writen - does not include IHDR, IDAT, END, perhaps yes PLTE </summary>
		/// <returns>THis is not a copy! Don't modify</returns>
		internal List<PngChunk> GetQueuedChunks() => queuedChunks;

	}
}
