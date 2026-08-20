// DialogueData.cs
using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public struct DialogueLine
{
    public string speakerName;      // 말하는 사람 이름
    [TextArea(3, 5)]
    public string dialogueText;     // 대사 내용
    public Sprite leftPortrait;     // 왼쪽 일러스트 (없으면 Null)
    public Sprite rightPortrait;    // 오른쪽 일러스트 (없으면 Null)
    // public AudioClip voiceClip;  // 나중에 음성이 추가될 수도 있으니 비워둠
}

[CreateAssetMenu(fileName = "New Dialogue", menuName = "Story/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    public List<DialogueLine> lines; // 대사 목록
}