using System;

namespace FastRepository {
    /// <summary>
    /// 
    /// </summary>
    public class ImageStoredEventArgs:EventArgs
    {
        public string Identifier { get; }

        public ImageStoredEventArgs(string identifier)
        {
            this.Identifier = identifier;
        }
    }
}
