using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class MBMiniBoardMultiProgressItem : MonoBehaviour
    {
        private Animator _animator;
        private GameObject _itemNode;
        private GameObject _lock;
        private GameObject _arrow;
        private UIImageRes _icon;
        private UIImageRes _shadow;
        private TextMeshProUGUI _num;
        private int _id;
        private int _index;

        public void Init()
        {
            _animator = transform.GetComponent<Animator>();
            _itemNode = transform.Find("ItemNode").gameObject;
            _lock = transform.Find("Lock").gameObject;
            _arrow = transform.Find("ItemNode/Arrow").gameObject;
            transform.Access("ItemNode/Icon_back", out _shadow);
            transform.Access("ItemNode/Icon", out _icon);
            transform.Access("ItemNode/NumBg/Num", out _num);
        }

        public void SetUp(int id, int index, MiniBoardMultiActivity activity)
        {
            _id = id;
            _index = index;
            var obj = Game.Manager.objectMan.GetBasicConfig(_id);
            _icon.SetImage(obj.Icon);
            _shadow.SetImage(obj.BlackIcon);
            _num.text = (_index + 1).ToString();
            if (activity == Game.Manager.miniBoardMultiMan.CurActivity)
                gameObject.SetActive(Game.Manager.miniBoardMultiMan.GetCurInfoConfig()?.LevelItem.Count >= _index);
            else
                gameObject.SetActive(true);
            Refresh(activity);
        }

        public void Refresh(MiniBoardMultiActivity activity)
        {
            if (activity == null) return;
            transform.localScale = Vector3.one;

            if (!Game.Manager.activity.mapR.ContainsKey(activity))
            {
                _animator.SetBool("Unlock", _index <= activity.UnlockMaxLevel);
                _animator.SetBool("Hide", false);
                _arrow.SetActive(_index == activity.UnlockMaxLevel);
                transform.GetComponent<LayoutElement>().ignoreLayout = false;
                return;
            }

            if (Game.Manager.miniBoardMultiMan.GetCurInfoConfig().LevelItem.Count > _index)
            {
                _animator.SetBool("Unlock", Game.Manager.miniBoardMultiMan.IsItemUnlock(_id));
                _animator.SetBool("Hide", false);
                _arrow.SetActive(_index == Game.Manager.miniBoardMultiMan.GetCurUnlockItemMaxLevel());
                transform.GetComponent<LayoutElement>().ignoreLayout = false;
            }
            else if (Game.Manager.miniBoardMultiMan.GetCurInfoConfig().LevelItem.Count == _index)
            {
                _animator.SetBool("Unlock", false);
                _animator.SetBool("Hide", true);
                transform.GetComponent<LayoutElement>().ignoreLayout = false;
            }
            else
            {
                transform.GetComponent<LayoutElement>().ignoreLayout = true;
                transform.localScale = Vector3.zero;
            }
        }

        public void Unlock()
        {
            _animator.SetTrigger("Punch");
        }
    }
}