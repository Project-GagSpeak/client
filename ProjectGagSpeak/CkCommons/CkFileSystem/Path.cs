namespace GagSpeak.CkCommons.FileSystem;

public partial class CkFileSystem<T>
{
    public interface IPath
    {
        public Folder Parent        { get; }
        public string Name          { get; }
        public uint   Identifier    { get; }
        public ushort IndexInParent { get; }
        public byte   Depth         { get; }
        public bool   State         { get; }

        public bool IsRoot
            => Depth == Folder.RootDepth;

        /// <summary> Obtain the full path of a filesystem path. </summary>
        public string FullName()
            => BaseFullName(this);

        /// <summary> Obtain all parents in order. </summary>
        public Folder[] Parents()
        {
            if (IsRoot || Parent.IsRoot)
                return Array.Empty<Folder>();

            var ret    = new Folder[Depth];
            var parent = Parent;
            for (var i = Depth - 1; i >= 0; i--)
            {
                ret[i] = parent;
                parent = parent.Parent;
            }

            return ret;
        }

        // Get the full name of the given path.
        internal static string BaseFullName(IPath path)
        {
            if (path.IsRoot)
                return string.Empty;

            var sb = new StringBuilder(path.Name.Length * path.Depth);
            Concat(path, sb, '/');
            return sb.ToString();
        }

        // Concatenate paths with a given separator.
        internal static bool Concat(IPath path, StringBuilder sb, char separator)
        {
            if (path.IsRoot)
                return false;

            if (Concat(path.Parent, sb, separator))
                sb.Append(separator);
            sb.Append(path.Name);
            return true;
        }
    }

    // Internally used to write things regardless of Folder/Leaf.
    internal interface IWritePath : IPath
    {
        public void SetParent(Folder parent);
        public void SetName(string name, bool fix = true);

        public void UpdateDepth();
        public void UpdateIndex(int idx);
        public void UpdateState(bool newState);
    }


    /// <summary> A search path just to be used for comparison between siblings. </summary>
    internal struct SearchPath : IWritePath
    {
        public Folder Parent        => null!;
        public string Name { get; private init; }
        public uint   Identifier    => 0;
        public byte   Depth         => 0;
        public ushort IndexInParent => 0;
        public bool   State         => false;

        public static implicit operator SearchPath(string path) => new() { Name = path };

        // For comparison purposes, we need to be a write path, not just a read path.
        // But these should never be called on a SearchPath.
        public void SetParent(Folder parent) => throw new NotImplementedException();
        public void SetName(string name, bool fix) => throw new NotImplementedException();
        public void UpdateDepth() => throw new NotImplementedException();
        public void UpdateIndex(int idx) => throw new NotImplementedException();
        public void UpdateState(bool newState) => throw new NotImplementedException();
    }
}
