using AForge.Imaging.Filters;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RitaglioPagine
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void inserisciPrimaImmagineToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    //Verifico che venga inserita una pagina sinistra
                    string fileName = openFileDialog1.FileName;
                    int indexOf_ = fileName.LastIndexOf("_");
                    int n = int.Parse(fileName.Substring(indexOf_ + 1, 4));
                    if (n % 2 == 0)
                        throw new System.ArgumentException("Inserire immagine di una pagina sinistra");
                    else
                        pictureBox1.Image = (Bitmap)System.Drawing.Image.FromFile(openFileDialog1.FileName);
                }
            }
            catch (Exception ex)
            {
                DialogResult result;
                result = MessageBox.Show(ex.Message);
            }

        }


        private void inserisciSecondaImmagineToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    //Verifico che venga inserita una pagina destra
                    string fileName = openFileDialog1.FileName;
                    int indexOf_ = fileName.LastIndexOf("_");
                    int n = int.Parse(fileName.Substring(indexOf_ + 1, 4));
                    if (n % 2 != 0)
                        throw new System.ArgumentException("Inserire immagine di una pagina destra");
                    else
                        pictureBox2.Image = (Bitmap)System.Drawing.Image.FromFile(openFileDialog1.FileName);

                }
            }
            catch (Exception ex)
            {
                DialogResult result;
                result = MessageBox.Show(ex.Message);
            }

        }


        private void ritaglioToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {


                /*
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();
                */

                Parallel.Invoke(() =>
                {
                    PrimaImmagine();
                },  // close first Action

                                 () =>
                                 {
                                     SecondaImmagine();
                                 } //close second Action


                             ); //close parallel.invoke
               /* 
                stopWatch.Stop();
                // Get the elapsed time as a TimeSpan value.
                TimeSpan ts = stopWatch.Elapsed;

                // Format and display the TimeSpan value.
                string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                    ts.Hours, ts.Minutes, ts.Seconds,
                    ts.Milliseconds / 10);
                MessageBox.Show(elapsedTime);
                */

            }
            catch (Exception ex)
            {
                DialogResult result;
                result = MessageBox.Show(ex.Message);
            }

        }
        private void PrimaImmagine()
        {
            try
            {
                //Ritaglio un po la foto base in modo da togliere eventuali sfocature che si trovano ai margini
                Rectangle rettangoloInziale = new Rectangle(0, 0, pictureBox1.Image.Width - 30, pictureBox1.Image.Height - 30);
                Crop ritIniz = new Crop(rettangoloInziale);
                Bitmap fotoBaseDestra = ritIniz.Apply((Bitmap)pictureBox1.Image);

                //Lavoro nella parte destra dell' immagine dato che l'indicatore di riferimento si torverà li 
                Crop ritaglioDX = new Crop(new Rectangle(fotoBaseDestra.Width - (fotoBaseDestra.Width / 3), 0, fotoBaseDestra.Width / 3, fotoBaseDestra.Height));
                Bitmap fotoDX = ritaglioDX.Apply(fotoBaseDestra);

                // creo il filtro per i grigi (BT709)
                Grayscale filter = new Grayscale(0.2125, 0.7154, 0.0721);
                Bitmap grayImageDx = filter.Apply(fotoDX);
                //Inserisco altri filtri per pulire l'immagine
                new BradleyLocalThresholding().ApplyInPlace(grayImageDx);
                new ContrastStretch().ApplyInPlace(grayImageDx);
                new Median().ApplyInPlace(grayImageDx);
                new Erosion3x3().ApplyInPlace(grayImageDx);

                //Riduco ancora l'immagine per migliorare le prestazioni e la velocita di elaborazione
                Bitmap newImage = (grayImageDx);
                Size sizen = new Size((newImage.Width), (newImage.Height / 5));
                Bitmap immagineDxRid = new Bitmap(newImage, sizen);

                Ritaglio ritMin = new Ritaglio();

                //Verifico se l'immagine è una pagina del libro oppure no
                if (ritMin.VerificaLato(immagineDxRid) == true)
                {
                    
                    ritMin.TrovaRettangoli(immagineDxRid);
                    // ritaglio la pagina in base al rettangolo restituito
                    Rectangle supporto = ritMin.TagliaBordoEsternoDestra();
                    //Aggiungo 20 px al bordo destro per far prendere parte della pagina accanto come stabilito dall'ICCU
                    Rectangle rectRit = new Rectangle(0, 0, ((fotoBaseDestra.Width) - (fotoBaseDestra.Width / 3)) + supporto.Right +20, fotoBaseDestra.Height);
                    Crop ritaglioMin = new Crop(rectRit);
                    Bitmap newImageMin = ritaglioMin.Apply(fotoBaseDestra);
                    Bitmap nuovaImmagineMin = newImageMin;
                    
                    //Ridimensiono di nuovo l'immagine sempre per una questione di prestazione
                    Size sizen2 = new Size((nuovaImmagineMin.Width / 5), (nuovaImmagineMin.Height / 5));
                    Bitmap immagineMinRidimensionata = new Bitmap(nuovaImmagineMin, sizen2);

                    //Applico i filtri all' immagine ridimensionata e ritagliata senza il residuo della pagina accanto 
                    Bitmap grayImageRitMin = filter.Apply((immagineMinRidimensionata));
                    new BradleyLocalThresholding().ApplyInPlace(grayImageRitMin);
                    new ContrastStretch().ApplyInPlace(grayImageRitMin);
                    new Median().ApplyInPlace(grayImageRitMin);
                    new Erosion3x3().ApplyInPlace(grayImageRitMin);

                    //Trovo il rettangolo della pagina principale 
                    ritMin.TrovaRettangoloFinale(grayImageRitMin);
                    Rectangle RettangoloBordoMin = ritMin.TaglioFinale();


                    //ritMax serve per includere nell'immagine finale quei dettagli o pezzi che fanno parte dell'immagine
                    //ma che sono di un colore scuro che con solo il processo di prima non sarebbero stati rilevati
                    //(copertina dell'libro)
                    Ritaglio ritMax = new Ritaglio();
                    //Tolgo un po di margine per evitare che sia rilevato qualche residuo che non faccia parte dell'immagine 
                    Crop ritaglioMax = new Crop(new Rectangle(rectRit.X, rectRit.Y, rectRit.Width - 70, rectRit.Height));
                    Bitmap newImageMax = ritaglioMax.Apply(nuovaImmagineMin);

                    Bitmap newImageMassima = (newImageMax);
                    Size sizen3 = new Size((newImageMax.Width / 5), (newImageMax.Height / 5));
                    Bitmap immagineMaxRidimensionata = new Bitmap(newImageMassima, sizen3);

                    Bitmap grayImageRitMax = filter.Apply((immagineMaxRidimensionata));
                    new ContrastStretch().ApplyInPlace(grayImageRitMax);
                    new Threshold(30).ApplyInPlace(grayImageRitMax);
                    new Median().ApplyInPlace(grayImageRitMax);
                    new Erosion3x3().ApplyInPlace(grayImageRitMax);

                    ritMax.TrovaRettangoloFinale(grayImageRitMax);
                    Rectangle RettangoloBordoMax = ritMax.TaglioFinale();

                    Ritaglio rit = new Ritaglio();
                    //Calcolo la differenza tra l' immagine del ritMin e ritMax
                    int Differnza = immagineMinRidimensionata.Width - immagineMaxRidimensionata.Width;
                    //Elaboro i due rettangoli e ottengo il rettangolo finale della pagina principale
                    Rectangle finale = rit.ConfrontaRettangoli(RettangoloBordoMin, RettangoloBordoMax, Differnza);
                    //Rapporto le misure trovate al  formato della pagina principale, aggiungendo uno spazio di 2 mm circa  
                    //ai lati come stabilito dall'ICCU
                    Rectangle finaleMisure = new Rectangle(finale.X * 5-20, finale.Y * 5-20, finale.Width * 5+20 , finale.Height * 5+40);
                    Crop ritaglioFinale = new Crop(finaleMisure);
                    Bitmap paginaPrincipaleSinistra = ritaglioFinale.Apply(nuovaImmagineMin);

                    pictureBox3.Image = paginaPrincipaleSinistra;

                }
                else
                {
                    pictureBox3.Image = pictureBox1.Image;
                }

            }
            catch (Exception ex)
            {
                DialogResult result;
                result = MessageBox.Show(ex.Message);
            }
        }
        private void SecondaImmagine()
        {
            try
            {
                //Ritaglio un po la foto base in modo da togliere eventuali sfocature che si trovano ai margini
                Rectangle rettangoloInziale = new Rectangle(0, 0, pictureBox2.Image.Width - 30, pictureBox2.Image.Height - 30);
                Crop ritIniz = new Crop(rettangoloInziale);
                Bitmap fotoBaseSinistra = ritIniz.Apply((Bitmap)pictureBox2.Image);

                //Lavoro nella parte sinistra dell' immagine dato che l'indicatore di riferimento si torverà li 
                Crop ritaglioSX = new Crop(new Rectangle(0, 0, fotoBaseSinistra.Width / 3, fotoBaseSinistra.Height));
                Bitmap fotoSX = ritaglioSX.Apply(fotoBaseSinistra);

                // creo il filtro per i grigi (BT709)
                Grayscale filter = new Grayscale(0.2125, 0.7154, 0.0721);
                Bitmap grayImageSx = filter.Apply(fotoSX);
                //Inserisco altri filtri per pulire l'immagine
                new BradleyLocalThresholding().ApplyInPlace(grayImageSx);
                new ContrastStretch().ApplyInPlace(grayImageSx);
                new Median().ApplyInPlace(grayImageSx);
                new Erosion3x3().ApplyInPlace(grayImageSx);

                //Riduco ancora l'immagine per migliorare le prestazioni e la velocita di elaborazione
                Bitmap newImage = (grayImageSx);
                Size sizen = new Size((newImage.Width), (newImage.Height / 5));
                Bitmap immagineSxRid = new Bitmap(newImage, sizen);

                Ritaglio ritMin = new Ritaglio();

                //Verifico se l'immagine è una pagina del libro oppure no
                if (ritMin.VerificaLato(immagineSxRid) == true)
                {
                    ritMin.TrovaRettangoli(immagineSxRid);
                    // ritaglio la pagina in base al rettangolo restituito
                    Rectangle supporto = ritMin.TagliaBordoEsternoSinistra();
                    //Aggiungo 20 px al bordo sinistro per far prendere parte della pagina accanto come stabilito dall'ICCU
                    Rectangle rectRit = new Rectangle(supporto.X-20, 0, fotoBaseSinistra.Width - (supporto.X+20), fotoBaseSinistra.Height);
                    Crop ritaglioMin = new Crop(rectRit);
                    Bitmap newImageMin = ritaglioMin.Apply(fotoBaseSinistra);
                    Bitmap nuovaImmagineMin = newImageMin;

                    //Ridimensiono di nuovo l'immagine sempre per una questione di prestazione
                    Size sizen2 = new Size((nuovaImmagineMin.Width / 5), (nuovaImmagineMin.Height / 5));
                    Bitmap immagineMinRidimensionata = new Bitmap(nuovaImmagineMin, sizen2);

                    //Applico i filtri all' immagine ridimensionata e ritagliata senza il residuo della pagina accanto 
                    Bitmap grayImageRitMin = filter.Apply((immagineMinRidimensionata));
                    new BradleyLocalThresholding().ApplyInPlace(grayImageRitMin);
                    new ContrastStretch().ApplyInPlace(grayImageRitMin);
                    new Median().ApplyInPlace(grayImageRitMin);
                    new Erosion3x3().ApplyInPlace(grayImageRitMin);

                    //Trovo il rettangolo della pagina principale 
                    ritMin.TrovaRettangoloFinale(grayImageRitMin);
                    Rectangle RettangoloBordoMin = ritMin.TaglioFinale();


                    //ritMax serve per includere nell'immagine finale quei dettagli o pezzi che fanno parte dell'immagine
                    //ma che sono di un colore scuro che con solo il processo di prima non sarebbero stati rilevati
                    //(copertina dell'libro)
                    Ritaglio ritMax = new Ritaglio();
                    //Tolgo un po di margine per evitare che sia rilevato qualche residuo che non faccia parte dell'immagine 
                    Crop ritaglioMax = new Crop(new Rectangle(rectRit.X + 70, rectRit.Y, rectRit.Width -(rectRit.X +70), rectRit.Height));
                    Bitmap newImageMax = ritaglioMax.Apply(nuovaImmagineMin);


                    Bitmap newImageMassima = (newImageMax);
                    Size sizen3 = new Size((newImageMax.Width / 5), (newImageMax.Height / 5));
                    Bitmap immagineMaxRidimensionata = new Bitmap(newImageMassima, sizen3);

                    Bitmap grayImageRitMax = filter.Apply((immagineMaxRidimensionata));
                    new ContrastStretch().ApplyInPlace(grayImageRitMax);
                    new Threshold(30).ApplyInPlace(grayImageRitMax);
                    new Median().ApplyInPlace(grayImageRitMax);
                    new Erosion3x3().ApplyInPlace(grayImageRitMax);

                    ritMax.TrovaRettangoloFinale(grayImageRitMax);
                    Rectangle RettangoloBordoMax = ritMax.TaglioFinale();


                    Ritaglio rit = new Ritaglio();
                    //Calcolo la differenza tra l' immagine del ritMin e ritMax
                    int Differnza = immagineMinRidimensionata.Width - immagineMaxRidimensionata.Width;
                    //Elaboro i due rettangoli e ottengo il rettangolo finale della pagina principale
                    Rectangle finale = rit.ConfrontaRettangoli(RettangoloBordoMin, RettangoloBordoMax, Differnza);
                    //Rapporto le misure trovate al  formato della pagina principale, aggiungendo uno spazio di 2 mm circa  
                    //ai lati come stabilito dall'ICCU
                    Rectangle finaleMisure = new Rectangle(finale.X * 5, finale.Y * 5 - 20, finale.Width * 5 + 20, finale.Height * 5 + 40);
                    Crop ritaglioFinale = new Crop(finaleMisure);
                    Bitmap paginaPrincipaleDestra = ritaglioFinale.Apply(nuovaImmagineMin);

                    pictureBox4.Image = paginaPrincipaleDestra;
                }
                else
                {
                    pictureBox4.Image = pictureBox2.Image;
                }
            }
            catch(Exception ex)
            {
                DialogResult result;
                result = MessageBox.Show(ex.Message);
            }
        }
    }
}
