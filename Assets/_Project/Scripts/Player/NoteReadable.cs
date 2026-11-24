using UnityEngine;

public class NoteReadable : MonoBehaviour
{
    [TextArea] public string noteText = "Una nota sin firma.";

    public void Read()
    {
        if (UIManager.Instance != null) UIManager.Instance.ShowNote(noteText);
    }
}
