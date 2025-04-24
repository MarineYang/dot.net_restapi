using System.Security.Cryptography.X509Certificates;

namespace webserver.Game
{
    public class Card
    {
        public int Value { get; set; } // 카드 숫자값 1~10
        public Card(int value) { Value = value; }
        public override string ToString() { return Value.ToString(); }
    }

}
