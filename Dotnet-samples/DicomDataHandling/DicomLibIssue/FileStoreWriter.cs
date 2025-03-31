using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DicomLibIssue
{
    internal class FileStoreWriter:IDicomWriter
    {
        private const string Location = @"C:\WorkingDirectory\DefectLogs";
        public async Task Store(Stream instanceStream)
        {
            var targetFile = Path.Combine(Location, Path.GetRandomFileName());
            await using var fileStream = new FileStream(targetFile, FileMode.CreateNew, FileAccess.Write);
            await instanceStream.CopyToAsync(fileStream);
        }
        
    }
}
