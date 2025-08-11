/*
 *@Author:chaoran.zhang
 *@Desc:装饰品详情
 *@Created Time:2024.05.23 星期四 17:00:33
 */

using EL;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static fat.conf.Data;

namespace FAT
{
    public class DecorateCell : MonoBehaviour
    {
        private TextMeshProUGUI _name;
        private UIImageRes _icon;
        private TextMeshProUGUI _num;
        private UIImageRes _coin;
        private GameObject _lock;
        private GameObject _unlock;
        private bool _enable;

        //1可购买
        //2可购买、货币不足
        //3已购买
        //4不可购买
        private UIStateGroup _group;
        private int _state;
        private int _id = -1;

        public void Init(int id, bool enable)
        {
            _enable = enable;
            _id = id;
            InitComp();
            InitInfo();
            SetGroup();
        }

        private void InitComp()
        {
            _name = transform.Find("Name").GetComponent<TextMeshProUGUI>();
            _icon = transform.Find("Icon").GetComponent<UIImageRes>();
            _num = transform.Find("Unlock_Btn/Reward/Num").GetComponent<TextMeshProUGUI>();
            _coin = transform.Find("Unlock_Btn/Reward/Icon").GetComponent<UIImageRes>();
            _lock = transform.Find("Unlock_Btn/Lock").gameObject;
            _unlock = transform.Find("Unlock_Btn/Unlock").gameObject;
            transform.AddButton("Unlock_Btn", ClickBuy);
            transform.AddButton("View_Btn", ClickView);
            _group = transform.GetComponent<UIStateGroup>();
            _lock.transform.GetChild(0).GetComponent<AnimationEvent>()
                .SetCallBack("Start", () => { Game.Manager.decorateMan.PlayAnim(); });
            _lock.transform.GetChild(0).GetComponent<AnimationEvent>()
                .SetCallBack(AnimationEvent.AnimationTrigger, () =>
                {
                    Game.Manager.audioMan.TriggerSound("DecorateUnlock");
                    GetComponent<Animation>().Play();
                });
            _unlock.GetComponent<AnimationEvent>()
                .SetCallBack(AnimationEvent.AnimationEnd, MessageCenter.Get<MSG.DECORATE_ANIM_END>().Dispatch);
            transform.GetComponent<AnimationEvent>().SetCallBack(AnimationEvent.AnimationTrigger, SetGroup);
        }

        private void InitInfo()
        {
            var conf = GetEventDecorateInfo(_id);
            _name.text = I18N.Text(conf.Name);
            _icon.SetImage(conf.Icon);
            _num.text = conf.Price.ToString();
            _coin.SetImage(GetObjBasic(conf.TokenID).Icon);
        }

        private int CheckGroupState()
        {
            var conf = GetEventDecorateInfo(_id);
            if (_enable)
            {
                if (Game.Manager.decorateMan.CheckDecorationUnlock(_id))
                {
                    return 2;
                }
                else
                {
                    return conf.Price > Game.Manager.decorateMan.Score ? 1 : 0;
                }
            }
            else
            {
                return 3;
            }
        }

        private void SetGroup()
        {
            _state = CheckGroupState();
            switch (_state)
            {
                case 0:
                case 1:
                {
                    _group.Select(_state);
                    _coin.gameObject.SetActive(true);
                    _lock.SetActive(false);
                    break;
                }
                case 2:
                {
                    _group.Select(_state);
                    _coin.gameObject.SetActive(false);
                    _lock.SetActive(false);
                    break;
                }
                case 3:
                {
                    _group.Select(_state);
                    _coin.gameObject.SetActive(false);
                    _lock.SetActive(true);
                    _lock.transform.GetChild(0).GetComponent<Animation>().Play("eff_lock_close");
                    break;
                }
            }
        }

        private void ClickBuy()
        {
            if (_id < 0) return;
            var conf = GetEventDecorateInfo(_id);
            if (!Game.Manager.mapSceneMan.Select(conf.IslandId, conf.BuildId))
            {
                return;
            }
            UIManager.Instance.Visible(Game.Manager.decorateMan.Panel, false);
        }

        private void ClickView()
        {
            if (_state != 0 && _state != 1)
                return;
            var conf = GetEventDecorateInfo(_id);
            Game.Manager.mapSceneMan.Select(conf.IslandId, conf.BuildId);
            UIManager.Instance.Visible(Game.Manager.decorateMan.Panel, false);
        }

        public void Refresh(bool enable)
        {
            if (_enable != enable)
            {
                _enable = enable;
                Game.Manager.decorateMan.AnimList.Add((5, PlayEnableAnim));
                Game.Manager.decorateMan.CurAnimNum++;
            }
            else
            {
                if (_state != CheckGroupState())
                {
                    _state = CheckGroupState();
                    switch (_state)
                    {
                        case 2:
                        {
                            Game.Manager.decorateMan.AnimList.Add((1, PlayUnlockAnim));
                            break;
                        }
                        default:
                        {
                            SetGroup();
                            break;
                        }
                    }
                }
                else
                {
                    if (_state == 2)
                    {
                        SetGroup();
                    }
                }
            }
        }

        private void PlayUnlockAnim()
        {
            SetGroup();
            _unlock.GetComponent<Animation>().Play();
        }

        private void PlayEnableAnim()
        {
            if(_lock.activeInHierarchy)
                _lock.transform.GetChild(0).GetComponent<Animation>().Play("UIDecoratePanel_lock_open");
        }


        public bool IsEnable()
        {
            return _state == 0;
        }

        public Transform GetButtonTrans()
        {
            return transform.Find("Unlock_Btn");
        }
    }
}