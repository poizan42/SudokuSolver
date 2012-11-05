using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PoiTech.SudokuSolver
{
    public struct SudokuValues : ICollection<byte>
    {
        public static readonly SudokuValues None = new SudokuValues();
        public static readonly SudokuValues All = new SudokuValues(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 });

        private int val;

        private SudokuValues(int val)
        {
            this.val = val;
        }

        public SudokuValues(IEnumerable<byte> vals = null)
        {
            val = 0;
            if (vals == null)
                return;
            foreach (byte v in vals)
                val |= 1 << v;
        }

        public SudokuValues(SudokuValues sv)
        {
            this.val = sv.val;
        }

        public bool Contains(byte i)
        {
            return (val & (1 << i)) != 0;
        }

        public static SudokuValues operator +(SudokuValues a, byte b)
        {
            return new SudokuValues(a.val | (1 << b));
        }

        public static SudokuValues operator |(SudokuValues a, byte b)
        {
            return a + b;
        }

        public static SudokuValues operator |(SudokuValues a, SudokuValues b)
        {
            return new SudokuValues(a.val | b.val);
        }

        public static SudokuValues operator +(SudokuValues a, SudokuValues b)
        {
            return new SudokuValues(a.val | b.val);
        }

        public static SudokuValues operator -(SudokuValues a, byte b)
        {
            return new SudokuValues(a.val & ~(1 << b));
        }

        public static SudokuValues operator -(SudokuValues a, SudokuValues b)
        {
            return new SudokuValues(a.val & ~(b.val));
        }

        public static bool operator &(SudokuValues a, byte b)
        {
            return a.Contains(b);
        }

        public static SudokuValues operator &(SudokuValues a, SudokuValues b)
        {
            return new SudokuValues(a.val & b.val);
        }

        public static bool operator ==(SudokuValues a, SudokuValues b)
        {
            return a.val == b.val;
        }

        public static bool operator !=(SudokuValues a, SudokuValues b)
        {
            return a.val != b.val;
        }

        public void Add(byte item)
        {
            this.val = (this + item).val;
        }

        public void Clear()
        {
            val = 0;
        }

        public void CopyTo(byte[] array, int arrayIndex)
        {
            new List<byte>(GetEnumerable()).CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get
            {
                return (val >> 8) + ((val >> 7) & 1) + ((val >> 6) & 1) + ((val >> 5) & 1) +
                     ((val >> 4) & 1) + ((val >> 3) & 1) + ((val >> 2) & 1) + ((val >> 1) & 1) +
                     (val & 1);
            }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(byte item)
        {
            if (!Contains(item))
                return false;
            this.val = (this - item).val;
            return true;
        }

        private IEnumerable<byte> GetEnumerable()
        {
            for (byte v = 0; v < 9; v++)
            {
                if (Contains(v))
                    yield return v;
            }
        }

        public IEnumerator<byte> GetEnumerator()
        {
            return GetEnumerable().GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerable().GetEnumerator();
        }

        public override bool Equals(object obj)
        {
            return obj is SudokuValues && ((SudokuValues)obj).val == val;
        }

        public override int GetHashCode()
        {
            return val;
        }

        public override string ToString()
        {
            return "{" + String.Join(", ", this) + "}";
        }

        public bool IsEmpty
        {
            get
            {
                return val == 0;
            }
        }
    }
}
