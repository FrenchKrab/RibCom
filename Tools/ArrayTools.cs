using System;
using System.Collections.Generic;
using System.Text;

namespace RibCom.Tools
{
    public static class ArrayTools
    {

        /// <summary>
        /// Extract a subarray from a given array.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="arr"></param>
        /// <param name="index"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static T[] SubArray<T>(this T[] arr, int index, int length)
        {
            T[] result = new T[length];
            Array.Copy(arr, index, result, 0, length);
            return result;
        }

        /// <summary>
        /// Expand a byte array to have the wanted amount of bytes. (little endian, append at the end)
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="wantedBytes"></param>
        /// <returns></returns>
        public static byte[] ExpandByteArray(byte[] bytes, int wantedBytes)
        {
            byte[] newArray = new byte[wantedBytes];
            Buffer.BlockCopy(bytes, 0, newArray, 0, bytes.Length);
            return newArray;
        }

        public static void LittleEndianToLocalEndian(this byte[] bytes)
        {
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
        }

        public static void LocalEndianToLittleEndian(this byte[] bytes)
        {
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
        }



    }
}
