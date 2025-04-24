namespace webserver.Game
{
    public class Deck
    {
        public Queue<Card> Cards { get; private set; } = new Queue<Card>();

        public void Initialize()
        {
            Cards.Clear();
            for (int  i = 0;  i <= 10;  i++)
            {
                Cards.Enqueue(new Card(i));
            }
        }

        public void Shuffle()
        {
            var list = Cards.ToList();
            Random rng = new Random();

            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                Card temp = list[k];
                list[k] = list[n];
                list[n] = temp;
            }

            Cards = new Queue<Card>(list);
        }

        // 맨 위 카드 뽑기
        public Card DrawCard()
        {
            return Cards.Count > 0 ? Cards.Dequeue() : null;
        }

        // 카드 추가하기 (덱 맨 뒤에)
        public void AddCard(Card card)
        {
            Cards.Enqueue(card);
        }

        // 여러 카드 한 번에 추가
        public void AddCards(IEnumerable<Card> cards)
        {
            foreach (var card in cards)
            {
                Cards.Enqueue(card);
            }
        }

        // 덱에 남은 카드 수
        public int Count => Cards.Count;
    }
}
