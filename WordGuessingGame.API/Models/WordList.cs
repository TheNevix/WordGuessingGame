namespace WordGuessingGame.API.Models
{
    public class WordList
    {
        public List<string> Words { get; set; }

        public WordList(IEnumerable<string> words)
        {
            Words = words.ToList();
        }
    }
}
