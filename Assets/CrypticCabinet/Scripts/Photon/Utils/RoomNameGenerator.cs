// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Text;
using UnityEngine;

namespace CrypticCabinet.Photon.Utils
{
    /// <summary>
    ///     Utility class to generate a random room name using numeric digits.
    /// </summary>
    public static class RoomNameGenerator
    {
        private const string DATA_SOURCE = "0123456789";

        /// <summary>
        ///     Generate a random room name of the given length
        /// </summary>
        /// <param name="length">How many characters should be in the name</param>
        /// <returns></returns>
        public static string GenerateRoom(int length = 6)
        {
            var dataLength = DATA_SOURCE.Length;
            var sb = new StringBuilder(length);
            for (var i = 0; i < length; ++i)
            {
                var index = Random.Range(0, dataLength);
                _ = sb.Append(DATA_SOURCE[index]);
            }

            return sb.ToString();
        }
    }
}