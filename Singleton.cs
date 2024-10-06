namespace PeerReviewClient
{
    using System;

    public class Singleton
    {
        private static readonly Lazy<Singleton> _instance = new Lazy<Singleton>(() => new Singleton());

        private DateTime _startTime;

        private Singleton()
        {
            _startTime = DateTime.MinValue;
        }

        public static Singleton Instance
        {
            get
            {
                return _instance.Value;
            }
        }

        /// <summary>
        /// Timer che mi serve per fare il controllo per la stampa del titolo di ogni sezione
        /// Se il tempo passato è maggiore di 200ms allora posso stampare il titolo
        /// Se no vuol dire che ho richiamato io la funzione da codice e quindi non devo stampare il titolo
        /// </summary>
        public void SetTimer()
        {
            _startTime = DateTime.Now;
        }

        public bool IsTimeMillisecondsPassed(int t = 200)
        {
            return (DateTime.Now - _startTime).TotalMilliseconds >= t;
        }
        
    }


}
