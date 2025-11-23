using Guna.UI2.WinForms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace INVOP.Controles
{
    public partial class FilaRestriccion : UserControl
    {
        public List<Guna2TextBox> Coeficientes { get; private set; } = new List<Guna2TextBox>();
        public FilaRestriccion()
        {
            InitializeComponent();
        }
        public void AgregarVariable(int indiceVariable)
        {
            Guna2TextBox txt = new Guna2TextBox();
            txt.PlaceholderText = $"x{indiceVariable}";
            txt.Width = 60;
            txt.Height = 35;
            txt.Margin = new Padding(5);

            // Estilo Guna (básico)
            txt.BorderRadius = 5;

            pnlContenedor.Controls.Add(txt); // Lo metemos al panel visual
            Coeficientes.Add(txt);             // Lo guardamos en la lista lógica
            pnlContenedor.Controls.SetChildIndex(guna2ComboBox1, pnlContenedor.Controls.Count - 1);
            pnlContenedor.Controls.SetChildIndex(guna2TextBox3, pnlContenedor.Controls.Count - 1);
            pnlContenedor.Controls.SetChildIndex(guna2Button1, pnlContenedor.Controls.Count - 1);
        }
        public (List<double>, string, double) ObtenerDatos()
        {
            var valores = new List<double>();
            foreach (var txt in Coeficientes)
            {
                double.TryParse(txt.Text, out double val); // Si está vacío devuelve 0
                valores.Add(val);
            }

            string signo = guna2ComboBox1.Text; // El <=, =, >=
            double.TryParse(guna2TextBox3.Text, out double rhs);

            return (valores, signo, rhs);
        }
    }
}
