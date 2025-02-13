namespace GagSpeak.CkCommons.FileSystem;

public partial class FileSystem<T>
{
    /// <summary> The structure for folder objects within the file system. </summary>
    /// <remarks> Folders can hold children and other folders. </remarks>
    public class Folder : IWritePath
    {
        internal const byte RootDepth = byte.MaxValue;

        /// <summary> The folder internally keeps track of total descendants and total leaves. </summary>
        public int TotalDescendants { get; internal set; } = 0;
        public int TotalLeaves      { get; internal set; } = 0;

        internal readonly List<IWritePath> Children = new();

        public int TotalChildren
            => Children.Count;

        public Folder Parent        { get; internal set; }
        public string Name          { get; private set; }
        public uint   Identifier    { get; }
        public ushort IndexInParent { get; internal set; }
        public byte   Depth         { get; internal set; }
        public bool   State         { get; internal set; }

        public bool IsRoot
            => Depth == RootDepth;

        void IWritePath.SetParent(Folder parent)
            => Parent = parent;

        void IWritePath.SetName(string name, bool fix)
            => Name = fix ? name.FixName() : name;

        void IWritePath.UpdateDepth()
        {
            var oldDepth = Depth;
            Depth = unchecked((byte)(Parent.Depth + 1));
            if (Depth == oldDepth)
                return;

            foreach (var desc in GetWriteDescendants())
                desc.UpdateDepth();
        }

        void IWritePath.UpdateIndex(int index)
        {
            if (index < 0)
                index = Parent.Children.IndexOf(this);
            IndexInParent = (ushort)(index < 0 ? 0 : index);
        }

        public void UpdateState(bool state)
            => State = state;

        public Folder(Folder parent, string name, uint identifier)
        {
            Parent     = parent;
            Name       = name.FixName();
            Identifier = identifier;
        }

        public IEnumerable<Folder> GetSubFolders()
            => Children.OfType<Folder>();

        public IEnumerable<Leaf> GetLeaves()
            => Children.OfType<Leaf>();


        /// <summary> Iterate through all direct children in sort order. </summary>
        /// <param name="mode"> The defined sorting mode you have set for folders. </param>
        /// <returns> The children of the folder in the defined sort order. </returns>
        public IEnumerable<IPath> GetChildren(ISortMode<T> mode)
            => mode.GetChildren(this);

        /// <summary> Iterate through all Descendants in sort order, not including the folder itself. </summary>
        /// <param name="mode"> The defined sorting mode you have set for folders. </param>
        /// <returns> The descendants of the folder in the defined sort order. </returns>
        public IEnumerable<IPath> GetAllDescendants(ISortMode<T> mode)
        {
            return GetChildren(mode).SelectMany(p => p is Folder f
                ? f.GetAllDescendants(mode).Prepend(f)
                : Array.Empty<IPath>().Append(p));
        }

        internal IEnumerable<IWritePath> GetWriteDescendants()
        {
            return Children.SelectMany(p => p is Folder f
                ? f.GetWriteDescendants().Prepend(f)
                : Array.Empty<IWritePath>().Append(p));
        }

        public string FullName()
            => IPath.BaseFullName(this);

        public override string ToString()
            => FullName();


        /// <summary> Creates the specific root element. </summary>
        /// <returns> The root Folder. </returns>
        /// <remarks> The name is set to empty due to it being fixed in the constructor. </remarks>
        internal static Folder CreateRoot()
            => new(null!, "_", 0)
            {
                Name  = string.Empty,
                Depth = RootDepth,
            };
    }
}
