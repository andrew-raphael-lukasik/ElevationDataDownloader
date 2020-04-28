using System.Collections.Generic;
using IO = System.IO;

namespace Pngcs.Chunks
{
	/// <summary>
	/// Represents a instance of a PNG chunk
	/// </summary>
	/// <remarks>
	/// Concrete classes should extend <c>PngChunkSingle</c> or <c>PngChunkMultiple</c>
	/// 
	/// Note that some methods/fields are type-specific (GetOrderingConstraint(), AllowsMultiple())
	/// some are 'almost' type-specific (Id,Crit,Pub,Safe; the exception is <c>PngUKNOWN</c>), 
	/// and some are instance-specific
	/// 
	/// Ref: http://www.libpng.org/pub/png/spec/1.2/PNG-Chunks.html
	/// </remarks>
	public abstract class PngChunk
	{

		/// <summary> 4 letters. The Id almost determines the concrete type (except for PngUKNOWN) </summary>
		public readonly string Id;

		/// <summary> Standard basic properties, implicit in the Id </summary>
		public readonly bool Crit, Pub, Safe;

		/// <summary> Image basic info, mostly for some checks </summary>
		protected readonly ImageInfo ImgInfo;

		/// <summary> For writing. Queued chunks with high priority will be written as soon as possible </summary>
		public bool Priority { get; set; }

		/// <summary> Chunk group where it was read or writen </summary>
		public int ChunkGroup { get; set; }

		public int Length { get; set; } // merely informational, for read chunks
		public long Offset { get; set; } // merely informational, for read chunks

		/// <summary> Restrictions for chunk ordering, for ancillary chunks </summary>
		public enum ChunkOrderingConstraint
		{
			/// <summary> No constraint, the chunk can go anywhere </summary>
			NONE,
			/// <summary> Before PLTE (palette) - and hence, also before IDAT </summary>
			BEFORE_PLTE_AND_IDAT,
			/// <summary> After PLTE (palette), but before IDAT </summary>
			AFTER_PLTE_BEFORE_IDAT,
			/// <summary> Before IDAT (before or after PLTE) </summary>
			BEFORE_IDAT,
			/// <summary> Does not apply </summary>
			NA
		}

		/// <summary> Constructs an empty chunk </summary>
		protected PngChunk ( string id , ImageInfo imgInfo )
		{
			this.Id = id;
			this.ImgInfo = imgInfo;
			this.Crit = Pngcs.Chunks.ChunkHelper.IsCritical(id);
			this.Pub = Pngcs.Chunks.ChunkHelper.IsPublic(id);
			this.Safe = Pngcs.Chunks.ChunkHelper.IsSafeToCopy(id);
			this.Priority = false;
			this.ChunkGroup = -1;
			this.Length = -1;
			this.Offset = 0;
		}

		static Dictionary<string,System.Type> factoryMap = new Dictionary<string,System.Type>(){
			{ ChunkHelper.IDAT , typeof(PngChunkIDAT) } ,
			{ ChunkHelper.IHDR, typeof(PngChunkIHDR) } ,
			{ ChunkHelper.PLTE, typeof(PngChunkPLTE) } ,
			{ ChunkHelper.IEND, typeof(PngChunkIEND) } ,
			{ ChunkHelper.tEXt, typeof(PngChunkTEXT) } ,
			{ ChunkHelper.iTXt, typeof(PngChunkITXT) } ,
			{ ChunkHelper.zTXt, typeof(PngChunkZTXT) } ,
			{ ChunkHelper.bKGD, typeof(PngChunkBKGD) } ,
			{ ChunkHelper.gAMA, typeof(PngChunkGAMA) } ,
			{ ChunkHelper.pHYs, typeof(PngChunkPHYS) } ,
			{ ChunkHelper.iCCP, typeof(PngChunkICCP) } ,
			{ ChunkHelper.tIME, typeof(PngChunkTIME) } ,
			{ ChunkHelper.tRNS, typeof(PngChunkTRNS) } ,
			{ ChunkHelper.cHRM, typeof(PngChunkCHRM) } ,
			{ ChunkHelper.sBIT, typeof(PngChunkSBIT) } ,
			{ ChunkHelper.sRGB, typeof(PngChunkSRGB) } ,
			{ ChunkHelper.hIST, typeof(PngChunkHIST) } ,
			{ ChunkHelper.sPLT, typeof(PngChunkSPLT) } ,
			// extended
			{ PngChunkOFFS.ID, typeof(PngChunkOFFS) } ,
			{ PngChunkSTER.ID, typeof(PngChunkSTER) } ,
		};

		/// <summary> Registers a Chunk ID in the factory, to instantiate a given type </summary>
		/// <remarks> This can be called by client code to register additional chunk types </remarks>
		/// <param name="chunkId"></param>
		/// <param name="type">should extend PngChunkSingle or PngChunkMultiple</param>
		public static void FactoryRegister ( string chunkId , System.Type type ) => factoryMap.Add( chunkId , type );

		internal static bool IsKnown ( string id ) => factoryMap.ContainsKey(id);

		internal bool MustGoBeforePLTE () => GetOrderingConstraint()==ChunkOrderingConstraint.BEFORE_PLTE_AND_IDAT;

		internal bool MustGoBeforeIDAT ()
		{
			ChunkOrderingConstraint oc = GetOrderingConstraint();
			return oc==ChunkOrderingConstraint.BEFORE_IDAT
				|| oc==ChunkOrderingConstraint.BEFORE_PLTE_AND_IDAT
				|| oc==ChunkOrderingConstraint.AFTER_PLTE_BEFORE_IDAT;
		}

		internal bool MustGoAfterPLTE () => GetOrderingConstraint()==ChunkOrderingConstraint.AFTER_PLTE_BEFORE_IDAT;

		internal static PngChunk Factory ( ChunkRaw chunk , ImageInfo info )
		{
			PngChunk c = FactoryFromId( Pngcs.Chunks.ChunkHelper.ToString(chunk.IdBytes) , info );
			c.Length = chunk.Len;
			c.ParseFromRaw(chunk);
			return c;
		}
		
		/// <summary> Creates one new blank chunk of the corresponding type, according to factoryMap (PngChunkUNKNOWN if not known) </summary>
		/// <param name="cid">Chunk Id</param>
		internal static PngChunk FactoryFromId ( string cid , ImageInfo info )
		{
			PngChunk chunk = null;
			if( IsKnown(cid) )
			{
				System.Type t = factoryMap[cid];
				if( t==null ) UnityEngine.Debug.Log($"What?? {cid}");
				System.Reflection.ConstructorInfo cons = t.GetConstructor( new System.Type[]{ typeof(ImageInfo) } );
				object o = cons.Invoke( new object[]{ info } );
				chunk = (PngChunk)o;
			}
			if( chunk==null )
			{
				chunk = new PngChunkUNKNOWN( cid , info );
			}

			return chunk;
		}

		public ChunkRaw CreateEmptyChunk ( int len , bool alloc )
		{
			ChunkRaw chunk = new ChunkRaw( len , ChunkHelper.ToBytes(Id) , alloc );
			return chunk;
		}

		/* @SuppressWarnings("unchecked")*/
		public static T CloneChunk <T> ( T chunk , ImageInfo info ) where T : PngChunk
		{
			PngChunk cn = FactoryFromId( chunk.Id , info );
			if( cn.GetType()!=chunk.GetType() ) throw new System.Exception($"bad class cloning chunk: {cn.GetType()} {chunk.GetType()}");
			cn.CloneDataFromRead(chunk);
			return (T)cn;
		}

		internal void Write ( IO.Stream os )
		{
			ChunkRaw chunk = CreateRawChunk();
			if( chunk==null ) throw new System.Exception( $"null chunk ! creation failed for {this}");
			chunk.WriteChunk(os);
		}

		/// <summary> Basic info: Id, length, Type name </summary>
		public override string ToString () => $"chunk id= {Id} (len={Length} off={Offset}) c={GetType().Name}";

		/// <summary> Serialization. Creates a Raw chunk, ready for write, from this chunk content </summary>
		public abstract ChunkRaw CreateRawChunk ();

		/// <summary> Deserialization. Given a Raw chunk, just rad, fills this chunk content </summary>
		public abstract void ParseFromRaw ( ChunkRaw chunk );

		/// <summary> Override to make a copy (normally deep) from other chunk </summary>
		public abstract void CloneDataFromRead ( PngChunk other );

		/// <summary> This is implemented in PngChunkMultiple/PngChunSingle </summary>
		/// <returns>Allows more than one chunk of this type in a image</returns>
		public abstract bool AllowsMultiple ();

		/// <summary> Get ordering constrain </summary>
		public abstract ChunkOrderingConstraint GetOrderingConstraint();

	}
}
