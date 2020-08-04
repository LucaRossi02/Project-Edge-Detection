using AForge.Imaging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RitaglioPagine
{
    class Ritaglio
    {
        private Bitmap prova;
        private Bitmap provaFinale;
        private Blob[] blobs;
        private Blob[] blobsFinal;
        private List<Rectangle> rettangoliIndicatori = new List<Rectangle>();
        private List<Rectangle> rectanglesFinal = new List<Rectangle>();
        private Boolean thisSide = false;

        public Ritaglio()
        {

        }

        //Metodo che verifica se l'immagine è una pagina del libro oppure no
        public Boolean VerificaLato(Bitmap immagine)
        {
            Blob[] blobs1;
            List<Rectangle> presenzaRettangoli = new List<Rectangle>();
            List<Rectangle> verificaRettangoli = new List<Rectangle>();

            // Crea un istanza di un BlobCounter 
            BlobCounterBase bc1 = new BlobCounter();
            // Un set di filtri
            bc1.FilterBlobs = true;
            bc1.MinWidth = 5;
            bc1.MinHeight = 5;
            Bitmap verifica = new Bitmap(immagine);
            // Processo l'immagine
            bc1.ProcessImage(verifica);
            blobs1 = bc1.GetObjects(verifica, false);
            
            // processo i blob
            foreach (Blob blob in blobs1)
            {
                verificaRettangoli.Add(blob.Rectangle);
            }
            //Verifico se nell'immagine c'è un rettangolo "indicatore"
            foreach (Rectangle rect in verificaRettangoli)
            {
                if (rect.Height < 500/5 && rect.Width < 500/2)
                    presenzaRettangoli.Add(rect);        
            }
            //Se è presente n rettangolo indicatore vuol dire che l'immagine è una pagina del libro e restituisce
            //vero altrimenti falso
            if (presenzaRettangoli.Count == 0)
            {
                thisSide = false;
            }
            else
                thisSide = true;

            return thisSide;
        }

        //Metodo che da un immagine in input ricerca tutti i rettangoli presenti nell'immagine sotto un determinato filtro
        public Bitmap TrovaRettangoli(Bitmap immagine)
        {
            // Crea un istanza di un BlobCounter 
            BlobCounterBase bc = new BlobCounter();
            // Un set di filtri
            bc.FilterBlobs = true;
            bc.MinWidth = 5;
            bc.MinHeight = 5;
            Bitmap bit = new Bitmap(immagine);
            prova = bit;
            // Processo l'immagine
            bc.ProcessImage(prova);
            blobs = bc.GetObjects(prova, false);
            // processo i blob
            foreach (Blob blob in blobs)
            {
                rettangoliIndicatori.Add(blob.Rectangle);
            }
            //La funzione using ha solo funzione visiva(Non è necessaria per il funzionamento del codice)
            //Mi mostra in rosso quali rettangoli sono stati trovati
            using (var gfx = Graphics.FromImage(prova))
            {
                foreach (Rectangle rect in rettangoliIndicatori)
                {
                    if (rect.Height < 500/5 && rect.Width < 500/2)
                        gfx.FillRectangle(Brushes.Red, rect);
                }
                gfx.Flush();
            }
            return prova;
        }

        public Rectangle TagliaBordoEsternoSinistra()
        {
            List<Rectangle> listaRP = new List<Rectangle>();
            List<Rectangle> listaRGA = new List<Rectangle>();
            List<Rectangle> listaRGB = new List<Rectangle>();
            List<Rectangle> listaRAMin = new List<Rectangle>();
            List<Rectangle> listaRBMin = new List<Rectangle>();

            //Aggiunge ad listaRP tutti i rettangoli minori di 500/2x500/5
            foreach (Rectangle rect in rettangoliIndicatori)
            {
                if (rect.Height < 500 / 5 && rect.Width < 500 / 2)
                {
                    listaRP.Add(rect);
                }
            }
            //Aggiungo i rettangoli a due liste separate in base alla posizione se si trovano in cima alla pagina
            //o in fondo alla pagina
            foreach (Rectangle rect in listaRP)
            {
                if (rect.Y < (prova.Height / 4))
                {
                    listaRGA.Add(rect);
                }
                else if (rect.Y > prova.Height - (prova.Height / 4))
                {
                    listaRGB.Add(rect);
                }
            }

            //Oridino in  modo decrescente le due liste in base alla grandezza dei rettangoli
            var rmina = listaRGA.OrderByDescending(r => r.Height * r.Width).ToList();
            var rminb = listaRGB.OrderByDescending(r => r.Height * r.Width).ToList();

            //Se non c'è nessun rettangolo nella parte bassa si trova in cima quello "indicatore"
            if (rminb.Count == 0)
            {
                //In cima c'è sempre la possibilita di avere uno o due rettangoli "indicatori"
                if (rmina.Count == 1)
                {
                    listaRAMin.Add(rmina[0]);
                }
                else
                {
                    listaRAMin.Add(rmina[0]);
                    listaRAMin.Add(rmina[1]);
                }
                //Oridino in  modo decrescente la lista in base alla grandezza del punto x piu a destra del rettangolo
                var r2 = listaRAMin.OrderByDescending(r => r.Right).ToList();
                //Scelgo come indicatore il rettangolo che mi resitutisce piu immagine
                Rectangle ritaglioRettangoloAlto = new Rectangle(r2[r2.Count-1].Right, 0, prova.Width - (r2[r2.Count - 1].Right), prova.Height);
                return ritaglioRettangoloAlto;
            }
            //Se non c'è nessun rettangolo nella parte alta si trova in basso quello "indicatore"
            else if (rmina.Count == 0)
            {
                //In basso c'è sempre la possibilita di avere uno o due rettangoli "indicatori"
                if (rminb.Count == 1)
                {
                    listaRBMin.Add(rminb[0]);

                }
                else
                {
                    listaRBMin.Add(rminb[0]);
                    listaRBMin.Add(rminb[1]);
                }

                //Oridino in  modo decrescente la lista in base alla grandezza del punto della x più a destra del rettangolo
                var r2 = listaRBMin.OrderByDescending(r => r.Right).ToList();
                //Scelgo come indicatore il rettangolo che mi resitutisce piu immagine
                Rectangle ritaglioRettangoloBasso = new Rectangle(r2[r2.Count - 1].Right, 0, prova.Width - (r2[r2.Count - 1].Right), prova.Height);
                return ritaglioRettangoloBasso;
            }
            //Se ci sono rettangoli sia in alto che a in basso scelgo di lavorare nel punto in cui si trova il rettangolo con
            //la x maggiore dato che sarà lui il rettangolo "indicatore"
            else
            {
                if (rmina.Count == 1)
                {

                    listaRAMin.Add(rmina[0]);
                }
                else
                {
                    listaRAMin.Add(rmina[0]);
                    listaRAMin.Add(rmina[1]);
                }
                if (rminb.Count == 1)
                {
                    listaRBMin.Add(rminb[0]);

                }
                else
                {
                    listaRBMin.Add(rminb[0]);
                    listaRBMin.Add(rminb[1]);
                }
                //Oridino in  modo decrescente la lista in base alla grandezza del punto della x piu a destra del rettangolo
                var r2 = listaRAMin.OrderByDescending(r => r.Right).ToList();
                //Oridino in  modo decrescente la lista in base alla grandezza del punto della x piu a destra del rettangolo
                var r3 = listaRBMin.OrderByDescending(r => r.Right).ToList();

                if (r2[r2.Count - 1].Right <= r3[r3.Count - 1].Right)
                {
                    Rectangle ritaglioRettangoloAlto = new Rectangle(r2[r2.Count - 1].Right, 0, prova.Width - (r2[r2.Count - 1].Right), prova.Height);
                    return ritaglioRettangoloAlto;
                }
                else
                {
                    Rectangle ritaglioRettangoloBasso = new Rectangle(r3[r3.Count - 1].Right, 0, prova.Width - (r3[r3.Count - 1].Right), prova.Height);
                    return ritaglioRettangoloBasso;
                }
            }
            
        }

        //Metodo che restituisce il rettangolo a cui è stato tolto il residuo della pagina accanto
        public Rectangle TagliaBordoEsternoDestra()
            
        {
            List<Rectangle> listaRP = new List<Rectangle>();
            List<Rectangle> listaRGA = new List<Rectangle>();
            List<Rectangle> listaRGB = new List<Rectangle>();
            List<Rectangle> listaRAMin = new List<Rectangle>();
            List<Rectangle> listaRBMin = new List<Rectangle>();

            //Aggiunge ad listaRP tutti i rettangoli minori di 500/2x500/5
            foreach (Rectangle rect in rettangoliIndicatori)
            {
                if (rect.Height < 500/5  && rect.Width < 500/2 )
                {
                    listaRP.Add(rect);
                }
            }
            //Aggiungo i rettangoli a due liste separate in base alla posizione se si trovano in cima alla pagina
            //o in fondo alla pagina
            foreach (Rectangle rect in listaRP)
            {
                if (rect.Y < (prova.Height / 4))
                {
                    listaRGA.Add(rect);
                }
                else if (rect.Y > prova.Height - (prova.Height / 4))
                {
                    listaRGB.Add(rect);
                }
            }
            //Oridino in  modo decrescente le due liste in base alla grandezza dei rettangoli
            var rmina = listaRGA.OrderByDescending(r => r.Height * r.Width).ToList();
            var rminb = listaRGB.OrderByDescending(r => r.Height * r.Width).ToList();

            //Se non c'è nessun rettangolo nella parte bassa si trova in cima quello "indicatore"
            if (rminb.Count == 0)
            {
                //In cima c'è sempre la possibilita di avere uno o due rettangoli "indicatori"
                if (rmina.Count == 1)
                {

                    listaRAMin.Add(rmina[0]);
                }
                else
                {
                    listaRAMin.Add(rmina[0]);
                    listaRAMin.Add(rmina[1]);
                }
                //Oridino in  modo decrescente la lista in base alla grandezza del punto della x 
                var r2 = listaRAMin.OrderByDescending(r => r.X).ToList();
                //Scelgo come indicatore il rettangolo che mi resitutisce piu immagine
                Rectangle ritaglioRettangoloAlto = new Rectangle(0, 0, r2[0].X, prova.Height);
                return ritaglioRettangoloAlto;
            }
            //Se non c'è nessun rettangolo nella parte alta si trova in basso quello "indicatore"
            else if (rmina.Count == 0)
            {
                //In basso c'è sempre la possibilita di avere uno o due rettangoli "indicatori"
                if (rminb.Count == 1)
                {
                    listaRBMin.Add(rminb[0]);

                }
                else
                {
                    listaRBMin.Add(rminb[0]);
                    listaRBMin.Add(rminb[1]);
                }

                //Oridino in  modo decrescente la lista in base alla grandezza del punto della x 
                var r2 = listaRBMin.OrderByDescending(r => r.X).ToList();

                Rectangle ritaglioRettangoloBasso = new Rectangle(0, 0, r2[0].X, prova.Height);
                return ritaglioRettangoloBasso;
            }
            //Se ci sono rettangoli sia in alto che a in basso scelgo di lavorare nel punto in cui si trova il rettangolo con
            //la x maggiore dato che sarà lui il rettangolo "indicatore"
            else
            {
                if (rmina.Count == 1)
                {

                    listaRAMin.Add(rmina[0]);
                }
                else
                {
                    listaRAMin.Add(rmina[0]);
                    listaRAMin.Add(rmina[1]);
                }
                if (rminb.Count == 1)
                {
                    listaRBMin.Add(rminb[0]);

                }
                else
                {
                    listaRBMin.Add(rminb[0]);
                    listaRBMin.Add(rminb[1]);
                }
                //Oridino in  modo decrescente la lista in base alla grandezza del punto della x 
                var r2 = listaRAMin.OrderByDescending(r => r.X).ToList();
                //Oridino in  modo decrescente la lista in base alla grandezza del punto della x 
                var r3 = listaRBMin.OrderByDescending(r => r.X).ToList();

                if (r2[0].X >= r3[0].X)
                {
                    Rectangle ritaglioRettangoloAlto = new Rectangle(0, 0, r2[0].X, prova.Height);
                    return ritaglioRettangoloAlto;
                }
                else
                {
                    Rectangle ritaglioRettangoloBasso = new Rectangle(0, 0, r3[0].X, prova.Height);
                    return ritaglioRettangoloBasso;
                }
            }
        }

        //Metodo per trovare il rettangolo, che corrisponde alla pagina principale
        public Bitmap TrovaRettangoloFinale(Bitmap immagine)
        {
            // Crea un istanza di un BlobCounter 
            BlobCounterBase bc = new BlobCounter();
            // Un set di filtri
            bc.FilterBlobs = true;
            bc.MinWidth = 50/5;
            bc.MinHeight = 50/5;
            Bitmap bit = new Bitmap(immagine);
            provaFinale = bit;
            // Processo l'immagine
            bc.ProcessImage(provaFinale);
            blobsFinal = bc.GetObjects(provaFinale, false);
            // processo i blob
            foreach (Blob blob in blobsFinal)
            {
                rectanglesFinal.Add(blob.Rectangle);
            }
            //La funzione using ha solo funzione visiva(Non è necessaria per il funzionamento del codice)
            //Ma mostra in rosso quali rettangoli sono stati trovati
            using (var gfx = Graphics.FromImage(provaFinale))
            {
                foreach (Rectangle rect in rectanglesFinal)
                {
                    if (rect.Height < (provaFinale.Height - 10/5) && rect.Width < (provaFinale.Width - 10/5))
                        gfx.FillRectangle(Brushes.Red, rect);
                }
                gfx.Flush();
            }
            return provaFinale;
        }

        //Metodo che restituisce il rettangolo con le coordinate della pagina 
        public Rectangle TaglioFinale()
        {
            List<Rectangle> listaRettangoloFinale = new List<Rectangle>();

            //Prendo i rettangoli che sono piu piccoli dell'immagine e scelgo quello piu grande che corrisponde alla pagina
            foreach (Rectangle rect in rectanglesFinal)
            {
                if (rect.Height < (provaFinale.Height - 10/5) && rect.Width < (provaFinale.Width - 10/5))
                    listaRettangoloFinale.Add(rect);
            }

            var rettangoloFinale = listaRettangoloFinale.OrderByDescending(r => r.Height * r.Width).ToList();

            Rectangle ritaglioFinale = new Rectangle(rettangoloFinale[0].X, rettangoloFinale[0].Y, rettangoloFinale[0].Width, rettangoloFinale[0].Height);
            return ritaglioFinale;
        }

        //Metodo che confronta due rettangoli che rappresentano la stessa pagina in diverso modo, restituendo il rettangolo
        //finale da ritagliare della pagina data in input al programma
        public Rectangle ConfrontaRettangoli(Rectangle RettangloBordoMin, Rectangle RettangoloBordoMax, int Differenza)
        {
            int asseX;
            int asseY;
            int Larghezza;
            int Altezza;
            
            //Se la x di RettangoloBordoMin è minore di 1 lavoro con la pagina di destra
            //In ogni coordinata tranne la x prendo sempre il RettangoloBordo che ha piu immagine 
            if (RettangloBordoMin.X <= 5 / 5)
            {
                if (RettangloBordoMin.Y <= RettangoloBordoMax.Y)
                {
                    asseY = RettangloBordoMin.Y;
                }
                else
                {
                    asseY = RettangoloBordoMax.Y;
                }
                if (RettangloBordoMin.Width >= RettangoloBordoMax.Width + Differenza)
                {
                    Larghezza = RettangloBordoMin.Width;
                }
                else
                {
                    Larghezza = RettangoloBordoMax.Width + Differenza;
                }
                if (RettangloBordoMin.Bottom >= RettangoloBordoMax.Bottom)
                {
                    if (RettangloBordoMin.Y <= RettangoloBordoMax.Y)
                        Altezza = RettangloBordoMin.Height;
                    else
                        Altezza = RettangloBordoMin.Height + (RettangloBordoMin.Y - RettangoloBordoMax.Y);
                }
                else
                {
                    if (RettangloBordoMin.Y >= RettangoloBordoMax.Y)
                        Altezza = RettangoloBordoMax.Height;
                    else
                        Altezza = RettangoloBordoMax.Height + (RettangloBordoMin.Y - RettangoloBordoMax.Y);
                }
                //La x è di RettangoloBordoMin perche è il punto di esatto di riferimento 
                Rectangle ritaglioRettangoloFinale = new Rectangle(RettangloBordoMin.X, asseY, Larghezza, Altezza);
                return ritaglioRettangoloFinale;
            }
            //Lavoro con la pagina di sinistra
            //In ogni coordinata tranne la larghezza prendo sempre il RettangoloBordo che ha piu immagine 
            else
            {
                if (RettangloBordoMin.X <= RettangoloBordoMax.X)
                {
                    asseX = RettangloBordoMin.X;
                    //La larghezza è di RettangoloBordoMin perchè è il punto esatto di riferimento
                    Larghezza = RettangloBordoMin.Width;
                }
                else
                {
                    asseX = RettangoloBordoMax.X;
                    Larghezza = RettangloBordoMin.Width + (RettangloBordoMin.X - RettangoloBordoMax.X);
                }
                if (RettangloBordoMin.Y <= RettangoloBordoMax.Y)
                {
                    asseY = RettangloBordoMin.Y;
                }
                else
                {
                    asseY = RettangoloBordoMax.Y;
                }
                if (RettangloBordoMin.Bottom >= RettangoloBordoMax.Bottom)
                {
                    if (RettangloBordoMin.Y <= RettangoloBordoMax.Y)
                        Altezza = RettangloBordoMin.Height;
                    else
                        Altezza = RettangloBordoMin.Height + (RettangloBordoMin.Y - RettangoloBordoMax.Y);
                }
                else
                {
                    if (RettangloBordoMin.Y >= RettangoloBordoMax.Y)
                        Altezza = RettangoloBordoMax.Height;
                    else
                        Altezza = RettangoloBordoMax.Height + (RettangloBordoMin.Y - RettangoloBordoMax.Y);
                }

                Rectangle ritaglioRettangoloFinale = new Rectangle(asseX, asseY, Larghezza, Altezza);
                return ritaglioRettangoloFinale;

            }
        }
    }
}
