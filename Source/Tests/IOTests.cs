using System.IO;
using System.Linq;
using ModManager;
using RimTest;
using static RimTest.Assertion;

namespace Tests
{
    [TestSuite]
    public static class IOTests
    {
        [Test]
        public static void SanitizeValidFileName()
        {
            const string input = "validFileName1.jpg";
            Assert( input.SanitizeFileName() ).To.Be.EqualTo( input );
        }

        [Test]
        public static void SanitizeInvalidFileName()
        {
            const string input  = "invalidFileName::!.jpg";
            const string output = "invalidFileName__!.jpg";
            Assert( input.SanitizeFileName() ).To.Be.EqualTo( output );
        }

        [Test]
        public static void IgnoresNonXmlModlists()
        {
            const string nonModListFile1 = "thisIsNotAModlist.DStore";

            try
            {
                using ( File.Create( Path.Combine( ModListManager.BasePath, nonModListFile1 ) ) )
                {
                    var files = Directory.GetFiles( ModListManager.BasePath, "?.xml" );
                    var file1 = files.FirstOrDefault( f => f.EndsWith( nonModListFile1 ) );
                    Assert( file1 ).Null();
                }
            }
            finally
            {
                File.Delete( Path.Combine( ModListManager.BasePath, nonModListFile1 ));
            }
        }

        [Test]
        public static void CatchesXmlModlists()
        {
            const string nonModListFile2 = "thisIsAModlist.xml";

            try
            {
                using ( File.Create( Path.Combine( ModListManager.BasePath, nonModListFile2 ) ) )
                {
                    var files = Directory.GetFiles( ModListManager.BasePath, "*.xml" );
                    var file2 = files.FirstOrDefault( f => f.EndsWith( nonModListFile2 ) );
                    Assert( file2 ).Not.Null();
                }
            }
            finally
            {
                File.Delete( Path.Combine( ModListManager.BasePath, nonModListFile2 ));
            }
        }
    }
}
