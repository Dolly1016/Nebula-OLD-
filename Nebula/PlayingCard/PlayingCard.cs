using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Nebula.Trumps
{
    public enum Suit
    {
        SPADE,
        HEART,
        DIAMOND,
        CLUB
    }

    public class PlayingCard
    {
        private static Texture2D RankTexture, SuitTexture;
        private static Sprite[] RankSprite=new Sprite[13];
        private static Sprite[] SuitSprite = new Sprite[4];
        private static Sprite BaseSprite;

        private static Texture2D GetRankTexture()
        {
            if (RankTexture) return RankTexture;
            RankTexture = Helpers.loadTextureFromResources("Nebula.Resources.TrumpsRank.png");
            return RankTexture;
        }

        public static Sprite GetRankSprite(int rank)
        {
            if (RankSprite[rank]) return RankSprite[rank];
            RankSprite[rank] = Helpers.loadSpriteFromResources(GetRankTexture(), 100f, new Rect(0f, (float)rank / 13f, 1f, 1f / 13f));
            return RankSprite[rank];
        }

        private static Texture2D GetSuitTexture()
        {
            if (SuitTexture) return SuitTexture;
            SuitTexture = Helpers.loadTextureFromResources("Nebula.Resources.TrumpsSuit.png");
            return SuitTexture;
        }

        public static Sprite GetSuitSprite(int suit)
        {
            if (SuitSprite[suit]) return SuitSprite[suit];
            SuitSprite[suit] = Helpers.loadSpriteFromResources(GetSuitTexture(), 100f, new Rect((float)suit / 4f, 0f, 1f / 4f, 1f));
            return SuitSprite[suit];
        }

        public Sprite GetBaseSprite()
        {
            if (BaseSprite) return BaseSprite;
            BaseSprite = Helpers.loadSpriteFromResources("Nebula.Resources.TrumpsBase.png", 100f);
            return BaseSprite;
        }

        public Suit Suit { get; set; }
        //強さの順に並べ替えた数　ex: 2→0 K→11 A→12
        public int Rank { get; set; }

        public PlayingCard(Suit suit, int rank)
        {
            Suit = suit;
            Rank = rank;
        }

        /// <summary>
        /// 強さの順に並ぶ重複しない値を得る
        /// </summary>
        /// <returns></returns>
        public int GetOrder()
        {
            return Rank * 4 + (3-(int)Suit);
        }

        public static bool operator >=(PlayingCard card1, PlayingCard card2)
        {
            return card1.GetOrder() >= card2.GetOrder();
        }

        public static bool operator <=(PlayingCard card1, PlayingCard card2)
        {
            return card1.GetOrder() <= card2.GetOrder();
        }

        public static bool operator >(PlayingCard card1, PlayingCard card2)
        {
            return card1.GetOrder() > card2.GetOrder();
        }

        public static bool operator <(PlayingCard card1, PlayingCard card2)
        {
            return card1.GetOrder() < card2.GetOrder();
        }

        public static bool operator !=(PlayingCard card1, PlayingCard card2)
        {
            return card1.GetOrder() != card2.GetOrder();
        }

        public static bool operator ==(PlayingCard card1, PlayingCard card2)
        {
            return card1.GetOrder() == card2.GetOrder();
        }
    }

    public class PokerHand
    {
        public List<PlayingCard> CardList { get; private set; }

        public void Add(PlayingCard card)
        {
            CardList.Add(card);
            CardList.Sort((c1, c2) => { return c1.GetOrder() - c2.GetOrder(); });
        }

        public PokerHand()
        {
            CardList = new List<PlayingCard>();
        }
    }

    public class CardTable
    {
        public Transform Transform { get; private set; }
        public PokerHand CurrentHand { get; private set; }

        public CardTable(Transform Transform, PokerHand hand)
        {
            CurrentHand = hand;
        }

        public void Update()
        {

        }
    }

    public class CardGraphic
    {
        public PlayingCard PlayingCard { get; private set; }
        public CardTable Table { get; private set; }
        private int CurrentIndex;
        public bool ValidFlag { get; private set; }
        public bool Destroyed { get; private set; }

        private SpriteRenderer BaseRenderer,SuitUpperRenderer,SuitLowerRenderer,RankRenderer;

        public CardGraphic(CardTable table,PlayingCard playingCard,GameObject obj)
        {
            Table = table;
            PlayingCard = playingCard;
            ValidFlag = true;
            Destroyed = false;

            obj.transform.SetParent(table.Transform);

            BaseRenderer = obj.GetComponent<SpriteRenderer>();

            SuitUpperRenderer = UnityEngine.Object.Instantiate(obj, table.Transform).GetComponent<SpriteRenderer>();
            SuitLowerRenderer = UnityEngine.Object.Instantiate(obj, BaseRenderer.transform).GetComponent<SpriteRenderer>();
            RankRenderer = UnityEngine.Object.Instantiate(obj, BaseRenderer.transform).GetComponent<SpriteRenderer>();

            BaseRenderer.transform.position = Vector3.zero;

            SuitUpperRenderer.transform.position = new Vector3(-0.1f, 0.2f);
            SuitLowerRenderer.transform.position = new Vector3(0.1f, -0.2f);
            RankRenderer.transform.position = Vector3.zero;

            SuitUpperRenderer.color = new Color(1f, 0f, 0f, 1f);
            SuitLowerRenderer.color = new Color(1f, 0f, 0f, 1f);
            SuitLowerRenderer.transform.eulerAngles = new Vector3(0f, 0f, 180f);

            Update();
        }

        public void Update()
        {
            if (Destroyed) return;

            var list = Table.CurrentHand.CardList;
            bool foundFlag = false;

            if (list.Count <= CurrentIndex)
            {
                if (list[CurrentIndex] == PlayingCard) foundFlag = true;
            }

            if (!foundFlag)
                for (int i = 0; i < list.Count; i++)
                    if (list[i] == PlayingCard)
                    {
                        CurrentIndex = i;
                        foundFlag = true;
                        break;
                    }

            ValidFlag=foundFlag;

            var pos = BaseRenderer.transform.position;
            if (ValidFlag)
            {

                BaseRenderer.transform.position = new Vector3(
                    ((float)CurrentIndex - ((float)list.Count / 2f) - pos.x) * 0.5f*Time.deltaTime + pos.x,
                    (-pos.y) * 0.5f * Time.deltaTime + pos.y,
                    0f
                    );
            }
            else
            {
                BaseRenderer.transform.position = new Vector3(
                    pos.x,
                    (-0.2f-pos.y) * 0.1f + pos.y,
                    0f
                    );
                BaseRenderer.color = new Color(1f, 1f, 1f, BaseRenderer.color.a - Time.deltaTime);

                if (!(BaseRenderer.color.a > 0f))
                {
                    UnityEngine.GameObject.Destroy(BaseRenderer);
                    UnityEngine.GameObject.Destroy(SuitUpperRenderer);
                    UnityEngine.GameObject.Destroy(SuitLowerRenderer);
                    UnityEngine.GameObject.Destroy(RankRenderer);

                    Destroyed = true;
                }
            }
        }
    }
}
