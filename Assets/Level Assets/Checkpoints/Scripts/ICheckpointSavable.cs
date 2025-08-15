






public interface ICheckpointSavable
{
    string SaveKey { get; }                 // unique per component type/instance
    object CaptureState();                  // return a serializable struct/class
    void RestoreState(object state);        // apply it back
}