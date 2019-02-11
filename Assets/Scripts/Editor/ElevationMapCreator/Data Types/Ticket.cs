namespace ElevationMapCreator
{
	/// <summary>
	/// This is simple and not thread safe yet useful class to (for example) abort running tasks
	/// </summary>
	public class Ticket
	{
		bool _valid = true;
		public bool valid { get=>_valid; }
		public bool invalid { get=>_valid==false; }
		public void Invalidate ()=> this._valid = false;
		public static implicit operator bool ( Ticket ticket )=> ticket._valid;
	}

	/// <summary>
	/// This is simple and not thread safe yet useful class to (for example) abort running tasks and share some data back and forth
	/// </summary>
	public class Ticket<T> : Ticket where T : struct
	{
		public T value;
		public Ticket ( T value )
		{
			this.value = value;
		}
	}
}
