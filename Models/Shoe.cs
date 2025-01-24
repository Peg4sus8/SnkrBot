namespace SnkrBot.Models
{
    public class Shoe
    {
        public string? Name { get; set; }   //Nome della scarpa
        public string? Release { get; set; }    // Data di rilascio (come stringa per semplicità)
        public string? Img { get; set; }    //link dell'immagine
        public int Price { get; set; }  //Prezzo della scarpa
    }
}
