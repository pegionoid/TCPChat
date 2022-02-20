using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChatLogSpace : MonoBehaviour
{
    [SerializeField] public GameObject chatprefab;
    private List<ChatData> displayedChats;
    void Awake()
    {
        displayedChats = new List<ChatData>();
        foreach (Transform n in this.gameObject.transform.Find("Viewport/Content"))
        {
            GameObject.Destroy(n.gameObject);
        }
    }

    public void Add(List<ChatData> chats)
    {
        GameObject chatlogspace = this.gameObject;
        GameObject content = chatlogspace.transform.Find("Viewport/Content").gameObject;
        LayoutGroup layoutGroup = content.GetComponent<LayoutGroup>();
        foreach (ChatData c in chats)
        {
            if (displayedChats.Contains(c))
            {
                continue;
            }
            GameObject chat = Instantiate(chatprefab);
            chat.transform.GetComponent<Chat>().SetData(c);
            chat.transform.SetParent(content.transform, false);
            displayedChats.Add(c);
        }
        layoutGroup.CalculateLayoutInputVertical();
        layoutGroup.SetLayoutVertical();
        StartCoroutine("ScrollToBottom", chatlogspace.GetComponent<ScrollRect>());
    }

    private IEnumerator ScrollToBottom(ScrollRect sr)
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        sr.verticalNormalizedPosition = 0f;
    }
}
