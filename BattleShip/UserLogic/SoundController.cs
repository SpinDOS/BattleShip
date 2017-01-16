using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BattleShip.Shared;
using NAudio.Wave;

namespace BattleShip.UserLogic
{
    /// <summary>
    /// Class for controlling sounds of calling and provide call voice recording/playing
    /// </summary>
    internal sealed class SoundController : IDisposable
    {
        // playing device
        private readonly WaveOutEvent _waveOut;
        // recording device
        private readonly WaveInEvent _waveIn;
        // buffer to play sound
        private readonly BufferedWaveProvider _buf;

        //
        private volatile bool _isRecording = false;

        // beep sound to play while calling
        private readonly LoopStream _beepSound = new LoopStream(new WaveFileReader(BattleShip.Properties.Resources.BeepSound1));
        // ringtone to play while opponent calls you
        private readonly LoopStream _ringtoneSound = new LoopStream(new WaveFileReader(BattleShip.Properties.Resources.OpponentCallsSound));
        // end call sound to indicate end call or cancellation calling
        private readonly WaveStream _endCallSound = new WaveFileReader(BattleShip.Properties.Resources.EndCallSound);

        // single exception for any usage after dispose()
        private readonly ObjectDisposedException _disposedException = new ObjectDisposedException("SoundController is disposed");

        public SoundController()
        {
            // common format for recording and playing voice
            var format = new WaveFormat();
            // initialize fields
            _buf = new BufferedWaveProvider(format);
            _waveOut = new WaveOutEvent();
            _waveIn = new WaveInEvent {WaveFormat = format};
            _waveIn.DataAvailable += (sender, args) => SoundRecorded?.Invoke(this, args);
        }

        /// <summary>
        /// Volume of the playing device
        /// </summary>
        public float Volume
        {
            get { return _waveOut.Volume; }
            set { _waveOut.Volume = value; }
        }

        public bool IsDisposed { get; private set; } = false;

        /// <summary>
        /// The event is raised when sound recorded from microphone
        /// </summary>
        public event EventHandler<WaveInEventArgs> SoundRecorded;

        /// <summary>
        /// Add data for playing 
        /// </summary>
        public void PlaySound(DataEventArgs data)
        {
            if (IsDisposed)
                throw _disposedException;

            _buf.AddSamples(data.Data, data.Offset, data.Count);
        }

        /// <summary>
        /// Play ringtone when opponent is calling you
        /// </summary>
        public void PlayRingtone()
        {
            if (IsDisposed)
                throw _disposedException;

            _waveOut.Stop();
            _waveOut.Init(_ringtoneSound.FromStart());
            _waveOut.Play();
        }

        /// <summary>
        /// Play beeps when user calls to opponent
        /// </summary>
        public void PlayBeeps()
        {
            if (IsDisposed)
                throw _disposedException;

            _waveOut.Stop();
            _waveOut.Init(_beepSound.FromStart());
            _waveOut.Play();
        }

        /// <summary>
        /// Start recording
        /// </summary>
        public void StartCall()
        {
            if (IsDisposed)
                throw _disposedException;

            _waveOut.Stop();
            _buf.ClearBuffer();
            _waveOut.Init(_buf);
            _waveOut.Play();

            _waveIn.StartRecording();
            _isRecording = true;
        }

        /// <summary>
        /// Play end call sound
        /// </summary>
        public void PlayEndCallSound()
        {
            if (IsDisposed)
                throw _disposedException;

            _waveOut.Stop();
            _waveOut.Init(_endCallSound.FromStart());
            _waveOut.Play();
        }

        /// <summary>
        /// Ends call
        /// </summary>
        public void EndCall()
        {
            if (IsDisposed)
                throw _disposedException;

            if (_isRecording)
                _waveIn.StopRecording();

            _buf.ClearBuffer();

            PlayEndCallSound();
        }
        
        /// <summary>
        /// Stop playing/recording sounds and release all resources
        /// </summary>
        public void Dispose()
        {
            if (IsDisposed)
                return;
            IsDisposed = true;
            // if any sound is playing - play end call and then dispose
            if (_waveOut.PlaybackState == PlaybackState.Playing)
                ThreadPool.QueueUserWorkItem(o =>
                {
                    _waveOut.Stop();
                    _waveOut.Init(_endCallSound.FromStart());
                    _waveOut.Play();
                    Thread.Sleep(_endCallSound.TotalTime - _endCallSound.CurrentTime);
                    _waveOut.Dispose();
                });
            else // else dispose
                _waveOut.Dispose();
            _waveIn.Dispose();
            _buf.ClearBuffer();
        }

        /// <summary>
        /// Stream for looping playback
        /// </summary>
        private class LoopStream : WaveStream
        {
            // stream of real sound
            private readonly WaveStream _sourceStream;

            /// <summary>
            /// Creates a new Loop stream
            /// </summary>
            /// <param name="sourceStream">The stream to read from. Note: the Read method of this stream should return 0 when it reaches the end
            /// or else we will not loop to the start again.</param>
            public LoopStream(WaveStream sourceStream)
            {
                _sourceStream = sourceStream;
            }

            #region WaveStream implementation

            /// <summary>
            /// Return source stream's wave format
            /// </summary>
            public override WaveFormat WaveFormat => _sourceStream.WaveFormat;

            /// <summary>
            /// LoopStream simply returns
            /// </summary>
            public override long Length => _sourceStream.Length;

            /// <summary>
            /// LoopStream simply passes on positioning to source stream
            /// </summary>
            public override long Position
            {
                get { return _sourceStream.Position; }
                set { _sourceStream.Position = value; }
            }

            /// <summary>
            /// LoopStream simply reads bytes from source stream if the end has not been reached
            /// Else loops and reads bytes from start
            /// </summary>
            /// <returns></returns>
            public override int Read(byte[] buffer, int offset, int count)
            { // read count of bytes and write to buffer from offset position


                int totalBytesRead = 0;

                while (totalBytesRead < count)
                {
                    // try read
                    int bytesRead = _sourceStream.Read(buffer, offset + totalBytesRead, count - totalBytesRead);
                    // if the end has been reached
                    if (bytesRead == 0)
                    {
                        if (_sourceStream.Position == 0)
                        {
                            // something wrong with the source stream
                            break;
                        }
                        // loop
                        _sourceStream.Position = 0;
                    }
                    totalBytesRead += bytesRead;
                }
                return totalBytesRead;
            }

            #endregion
        }

    }

    public static class WaveStreamFromStartExtension
    {
        /// <summary>
        /// Set the position of wavestream to start and return it
        /// </summary>
        /// <param name="waveStream">WaveStream where to set position to start</param>
        /// <returns></returns>
        public static WaveStream FromStart(this WaveStream waveStream)
        {
            waveStream.Position = 0;
            return waveStream;
        }
    }
}
