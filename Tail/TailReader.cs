using System;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace Tail
{
    public interface IStreamFactory
    {
        Stream CreateStream();
    }

    public class FileStreamFactory : IStreamFactory
    {
        private readonly string _filename;
        public FileStreamFactory(string filename)
        {
            _filename = filename;
        }

        public Stream CreateStream()
        {
            return new BufferedStream(new FileStream(_filename,
                             FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
        }
    }

    /// <summary>
    /// Tail reader interface.
    /// </summary>
    public interface ITailReader
    {
        /// <summary>
        /// Reads the given file from the tail.  
        /// </summary>
        /// <returns>An observable collection enabling callers to subscribe to updates.</returns>
        IObservable<string> Read();

        /// <summary>
        /// Reads the given file from the tail.  
        /// </summary>
        /// <param name="offset">The position in the file to read from.</param>
        /// <returns>An observable collection enabling callers to subscribe to updates.</returns>
        IObservable<string> Read(long offset);
    }

    public class TailReader : ITailReader
    {
        private readonly double _timeToLiveInHours = 1;
        private readonly double _pollingIntervalInSeconds = 5;
        private Timer _timer;
        private bool _timeout = false;
        private readonly IStreamFactory _streamFactory;
        
        /// <summary>
        /// Creates a new instance of the tail reader.
        /// </summary>
        public TailReader(string filename): this(filename, 5, 1)
        {
        }

        /// <summary>
        /// Creates a new instance of the tail reader.
        /// </summary>
        /// <param name="streamFactory">Used to create the stream to process</param>
        public TailReader(IStreamFactory streamFactory) : this(streamFactory, 5, 1)
        {
        }

        /// <summary>
        /// Creates a new instance of the tail reader.
        /// </summary>
        /// <param name="filename">The full path of the file to process.</param>
        /// <param name="pollingIntervalInSeconds">Determines the polling interval in seconds</param>
        /// <param name="timeToLiveInHours">Determines how the reader will tail each</param>
        public TailReader(string filename, double pollingIntervalInSeconds, double timeToLiveInHours)
            :this(new FileStreamFactory(filename), pollingIntervalInSeconds, timeToLiveInHours)
        {
        }

        /// <summary>
        /// Creates a new instance of the tail reader.
        /// </summary>
        /// <param name="streamFactory">Used to create the stream to process</param>
        /// <param name="pollingIntervalInSeconds">Determines the polling interval in seconds</param>
        /// <param name="timeToLiveInHours">Determines how the reader will tail each</param>
        public TailReader(IStreamFactory streamFactory, double pollingIntervalInSeconds, double timeToLiveInHours)
        {
            _streamFactory = streamFactory;
            _pollingIntervalInSeconds = pollingIntervalInSeconds;
            _timeToLiveInHours = timeToLiveInHours;
            SetTimer();
        }

        private void SetTimer()
        {
            // create a timer with the required interval
            _timer = new Timer(_timeToLiveInHours * 60 * 60 * 1000);

            // hook up the Elapsed event for the timer
            _timer.Elapsed += (sender, e) => { _timeout = true; };
            _timer.AutoReset = false;
            _timer.Enabled = true;
        }

        private IObservable<string> ReadImpl(long? offset)
        {
            return Observable.Create(async (IObserver<string> observer) =>
            {
                // we want to be able to read the file
                // we want other processes to be able to write to the file
                using (var reader = new StreamReader(_streamFactory.CreateStream()))
                {
                    // start at the end of the file or at the specified offset
                    long lastOffset = offset ?? reader.BaseStream.Length;

                    while (!_timeout)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(_pollingIntervalInSeconds));

                        // if the file size has not changed, idle
                        if (reader.BaseStream.Length == lastOffset)
                        {
                            continue;
                        }

                        // seek to the last offset
                        reader.BaseStream.Seek(lastOffset, SeekOrigin.Begin);

                        // read out of the file until the EOF
                        string line = "";
                        while ((line = reader.ReadLine()) != null)
                        {
                            observer.OnNext(line);
                        }

                        // update the last offset
                        lastOffset = reader.BaseStream.Position;

                    }

                    return Disposable.Empty;
                }
            });
        }

        /// <summary>
        /// Reads the given file from the tail.  
        /// </summary>
        /// <param name="filename">Fully qualified path to the file to be read.</param>
        /// <param name="offset">The position in the file to read from.</param>
        /// <returns>An observable collection enabling callers to subscribe to updates.</returns>
        public IObservable<string> Read(long offset)
        {
            return ReadImpl(offset);
        }

        /// <summary>
        /// Reads the given file from the tail.  
        /// </summary>
        /// <param name="filename">Fully qualified path to the file to be read.</param>
        /// <returns>An observable collection enabling callers to subscribe to updates.</returns>
        public IObservable<string> Read()
        {
            return ReadImpl(null);
        }
    }
}
