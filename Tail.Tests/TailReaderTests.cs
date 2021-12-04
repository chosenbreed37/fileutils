using System;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Tail.Tests
{
    public class MemoryStreamFactory : IStreamFactory
    {
        private readonly MemoryStream _stream;

        public MemoryStreamFactory(MemoryStream stream)
        {
            _stream = stream;
        }

        public Stream CreateStream()
        {
            return _stream;
        }
    }

    [TestFixture]
    public class TailReaderTests
    {
        private ITailReader _sut;
        private MemoryStream _stream;
        private int PollingIntervalInMilliSeconds = 5;


        [SetUp]
        public void SetUp()
        {
            _stream = new MemoryStream();
            var pollingIntervalInSeconds = PollingIntervalInMilliSeconds / 1000.0;
            _sut = new TailReader(new MemoryStreamFactory(_stream), pollingIntervalInSeconds, timeToLiveInHours: 0.01);
        }

        [Test]
        public async Task Can_read_new_entry()
        {
            var expected = "Lorem ipsum dolor sit amet";
            var actual = "";
            var observable = _sut.Read();
            bool completed = false;
            observable.Subscribe(entry =>
            {
                actual = entry;
                completed = true;
            });

            var content = System.Text.Encoding.UTF8.GetBytes(
                $"{expected}\r\n");
            _stream.Write(content, 0, content.Length);

            while (!completed)
            {
                await Task.Delay(PollingIntervalInMilliSeconds * 2);
            }

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public async Task Can_handle_missing_carriage()
        {
            var expected = "Lorem ipsum dolor sit amet";
            var actual = "";
            var observable = _sut.Read();
            bool completed = false;
            observable.Subscribe(entry =>
            {
                actual = entry;
                completed = true;
            });

            var content = System.Text.Encoding.UTF8.GetBytes(
                $"{expected}\n");
            _stream.Write(content, 0, content.Length);

            while (!completed)
            {
                await Task.Delay(PollingIntervalInMilliSeconds * 2);
            }

            Assert.AreEqual(expected, actual);
        }
    }
}
