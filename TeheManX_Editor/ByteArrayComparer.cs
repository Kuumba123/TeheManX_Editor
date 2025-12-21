using System.Collections;
using System.Collections.Generic;

namespace TeheManX_Editor
{
    /// <summary>
    ///    An IEqualityComparer that compares two byte arrays to see if they
    ///    are equal to each other based on the value sequences contained within
    ///    the arrays.
    /// </summary>
    public class ByteArrayComparer : IEqualityComparer<byte[]>
    {
        //    Private backing field for the Default property below.
        private static ByteArrayComparer _default;

        /// <summary>
        ///    Default instance of <see cref = "ByteArrayComparer"/>
        /// </summary>
        public static ByteArrayComparer Default
        {
            get
            {
                if (_default == null)
                {
                    _default = new ByteArrayComparer();
                }

                return _default;
            }
        }

        /// <summary>
        ///    Tests for equality between two byte arrays based on their value
        ///    sequences.
        ///	<param name = "obj1">A byte array to test for equality against obj2.</param>
        /// <param name = "obj2">A byte array to test for equality againts obj1.</param>
        /// </summary>
        public bool Equals(byte[] obj1, byte[] obj2)
        {
            return StructuralComparisons.StructuralEqualityComparer.Equals(obj1, obj2);
        }

        /// <summary>
        ///    Gets a hash code to identify the given object.
        /// </summary>
        /// <param name = "obj">The byte array to generate a hash code for.</param>
        public int GetHashCode(byte[] obj)
        {
            return StructuralComparisons.StructuralEqualityComparer.GetHashCode(obj);
        }
    }
}
