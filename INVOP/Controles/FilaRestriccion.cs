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
        public event EventHandler FilaEliminada;
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
            txt.BorderRadius = 5;

            pnlContenedor.Controls.Add(txt);
            Coeficientes.Add(txt);
            pnlContenedor.Controls.SetChildIndex(guna2ComboBox1, pnlContenedor.Controls.Count - 1);
            pnlContenedor.Controls.SetChildIndex(guna2TextBox3, pnlContenedor.Controls.Count - 1);
            pnlContenedor.Controls.SetChildIndex(btnCerrar, pnlContenedor.Controls.Count - 1);
        }
        public (List<double>, string, double) ObtenerDatos()
        {
            var valores = new List<double>();
            foreach (var txt in Coeficientes)
            {
                double.TryParse(txt.Text, out double val);
                valores.Add(val);
            }

            string signo = guna2ComboBox1.Text;
            double.TryParse(guna2TextBox3.Text, out double rhs);

            return (valores, signo, rhs);
        }

        private void btnCerrar_Click(object sender, EventArgs e)
        {
            // Verifica si hay suscriptores y dispara el evento
            FilaEliminada?.Invoke(this, EventArgs.Empty);
        }
    }
}
