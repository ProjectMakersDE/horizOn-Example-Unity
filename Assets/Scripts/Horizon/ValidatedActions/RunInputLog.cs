using System.Collections.Generic;
using UnityEngine;

namespace SeagullStorm
{
    /// <summary>
    /// Compact input log of one run for Validated Actions. The server keeps only its SHA-256 at
    /// submit time and may ask for the raw bytes later (evidence), so the log must describe the run
    /// well enough to replay it and must stay within the 32 KB evidence limit.
    ///
    /// Format v1 (little endian, same layout as the Godot Seagull Storm):
    /// <list type="bullet">
    /// <item>header: u8 format version, u32 run seed</item>
    /// <item>events: u16 physics ticks since the previous event, u8 event code</item>
    /// <item>0x00 to 0x0F movement: bit 0 left, bit 1 right, bit 2 up, bit 3 down (written on change)</item>
    /// <item>0x10 to 0x1F level up choice, low 4 bits = button index</item>
    /// <item>0xFF run end</item>
    /// </list>
    /// A physics tick is one FixedUpdate while the run is playing (not paused, no level up choice).
    /// Only direction changes are written, so a three minute run stays a few kilobytes.
    /// </summary>
    public class RunInputLog
    {
        public const byte FormatVersion = 1;
        public const int HeaderSize = 5;
        public const int EventSize = 3;

        /// <summary>Evidence limit of the server (32 KB). Recording stops when the log is full.</summary>
        public const int MaxBytes = 32768;

        private const byte EventMove = 0x00;
        private const byte EventLevelUpChoice = 0x10;
        private const byte EventRunEnd = 0xFF;
        private const int MaxTickGap = 65535;

        private readonly List<byte> _events = new List<byte>(2048);
        private int _seed;
        private int _ticksSinceEvent;
        private int _lastMove = -1;
        private bool _ended;

        /// <summary>True when the log hit <see cref="MaxBytes"/> and later input was dropped.</summary>
        public bool Truncated { get; private set; }

        /// <summary>Size of the log in bytes including the header.</summary>
        public int Length => HeaderSize + _events.Count;

        /// <summary>Clears the log for a new run with the server seed of its ticket.</summary>
        public void Begin(int seed)
        {
            _events.Clear();
            _seed = seed;
            _ticksSinceEvent = 0;
            _lastMove = -1;
            _ended = false;
            Truncated = false;
        }

        /// <summary>
        /// Records the movement input of one physics tick. Writes an event only when the direction
        /// changes (or the tick gap would overflow).
        /// </summary>
        public void RecordMove(Vector2 input)
        {
            if (_ended) return;

            int code = EventMove;
            if (input.x < 0f) code |= 1;
            else if (input.x > 0f) code |= 2;
            if (input.y > 0f) code |= 4;
            else if (input.y < 0f) code |= 8;

            _ticksSinceEvent++;
            if (code != _lastMove || _ticksSinceEvent >= MaxTickGap)
            {
                _lastMove = code;
                WriteEvent((byte)code);
            }
        }

        /// <summary>Records which level up button (0 based) the player picked.</summary>
        public void RecordLevelUpChoice(int buttonIndex)
        {
            if (_ended) return;
            WriteEvent((byte)(EventLevelUpChoice | (buttonIndex & 0x0F)));
        }

        /// <summary>
        /// Writes the run end marker and returns the raw log (header plus events).
        /// Hash and upload exactly these bytes.
        /// </summary>
        public byte[] Finish()
        {
            if (!_ended)
            {
                // WriteEvent() always keeps room for this marker.
                AppendEvent(EventRunEnd);
                _ended = true;
            }

            var bytes = new byte[Length];
            bytes[0] = FormatVersion;
            bytes[1] = (byte)(_seed & 0xFF);
            bytes[2] = (byte)((_seed >> 8) & 0xFF);
            bytes[3] = (byte)((_seed >> 16) & 0xFF);
            bytes[4] = (byte)((_seed >> 24) & 0xFF);
            _events.CopyTo(bytes, HeaderSize);
            return bytes;
        }

        private void WriteEvent(byte code)
        {
            if (Length + 2 * EventSize > MaxBytes)
            {
                Truncated = true;
                return;
            }
            AppendEvent(code);
        }

        private void AppendEvent(byte code)
        {
            int ticks = Mathf.Min(_ticksSinceEvent, MaxTickGap);
            _events.Add((byte)(ticks & 0xFF));
            _events.Add((byte)((ticks >> 8) & 0xFF));
            _events.Add(code);
            _ticksSinceEvent = 0;
        }
    }
}
