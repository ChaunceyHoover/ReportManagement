using System;
using System.ComponentModel.DataAnnotations;

namespace ReportPortal.Controllers.Helpers {
	public class NullableDateTime {
		[Key]
		public DateTime _ping {
			get {
				return new DateTime(_ticks);
			}
			set {
				_ticks = _ping.Ticks;
			}
		}

		public long _ticks { get; set; }

		#region Constructors

		public NullableDateTime() {
			_ticks = 0;
		}
		
		public NullableDateTime(DateTime d) {
			_ticks = d.Ticks;
		}

		public NullableDateTime(long ticks) {
			_ticks = ticks;
		}

		#endregion

		#region Properties

		public bool IsSpecialDate {
			get { return (_ticks == 0); }
		}

		#endregion

		#region Public Methods

		public override string ToString() {
			if (_ticks == 0) {
				return DateTime.MinValue.ToString();
			} else {
				return (new DateTime(_ticks)).ToString("yyyy-mm-dd");
			}
		}

		#endregion

		#region Operators

		public static implicit operator NullableDateTime(DateTime d) {
			return new NullableDateTime(d.Ticks);
		}

		#endregion
	}
}
