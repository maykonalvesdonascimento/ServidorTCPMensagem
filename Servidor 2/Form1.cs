using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Servidor_2
{
    public partial class Form1 : Form
    {
        // Criei uma variável do tipo TCPlistener que permite que eu possa fazer uma comunicação na rede via protocolo TCP
        private TcpListener servidor;
        // Criei uma lista de TCP Client , com objetivos de armazenar os Clients que se conectar com o servidor
        private List<TcpClient> clientesConectados = new List<TcpClient>();
        public Form1()
        {
            InitializeComponent();
        }



        // método que tem o objetivo de aceitar novos clientes
        private async Task AceitarClientes()
        {
            //um loop infinito, pois ele vai ficar sempre procurando novos clientes 
            while (true)
            {
                //caso ele ache um novo cliente, ele vai adicionar na váriavel cliente
                var cliente = await servidor.AcceptTcpClientAsync();
                //adiciona na lista de TCP Client
                clientesConectados.Add(cliente);
                
                //preenche o listbox com a mensagem "Cliente Conectado" , assim temos uma mensagem 
                lstMensagens.Invoke(new Action(() => lstMensagens.Items.Add("Cliente conectado!")));
                //criando uma task ( é estilo as threads que eu expliquei na aula, ele vai executar essa função numa thread separada, sem parar a execução da thread principal), ele vai chamar a função Cliente , aonde vou explicar abaixo oq ela faz
                // ele irá criar uma thread para cada cliente
                Task.Run(() => Cliente(cliente));
            }
        }

        //Task cliente permite que o Servidor consiga se comunicar com o cliente , seja mandar mensagem para todos usuários conectados , receber mensagem
        private async Task Cliente(TcpClient cliente)
        {
            //estou pegando os dados de NetworkStream ( ele eh responsável para manter a comunicação entre esse cliente e o servidor)
            using (var stream = cliente.GetStream())
            // reader permite que ele possa ler o que o cliente mandar pra o servidor
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                string nomeCliente = null;
                // loop infinito pra ficar ouvindo tudo que o cliente enviar pra ele
                while (true)
                {
                    try
                    {
                        //ele espera receber uma mensagem para continuar
                        string mensagem = await reader.ReadLineAsync();
                        if (mensagem != null)
                        {
                            //esse IF só pra tratar o nome do cliente, pois na primeira mensagem , o cliente manda o nome do cliente, ao invés de mensagem
                            if (nomeCliente == null)
                            {
                                //aonde atribuimos a mensagem para o nomeCliente
                                nomeCliente = mensagem; 
                                // E colocamos na listbox , que o cliente foi conectado ( e o nome dele)
                                lstMensagens.Invoke(new Action(() => lstMensagens.Items.Add($"Cliente conectado: {nomeCliente}")));
                                continue; 
                            }

                            //Log da mensagem que o cliente digitar, para ficar registrado no servidor
                            lstMensagens.Invoke(new Action(() => lstMensagens.Items.Add($"{nomeCliente}: {mensagem}")));

                            //Executa o método que envia a mensagem para todos clientes
                            EnviarMensagemTodosClientes($"{nomeCliente}: {mensagem}", cliente);
                        }
                    }
                    catch
                    {
                        // Remove cliente desconectado
                        clientesConectados.Remove(cliente);
                        lstMensagens.Invoke(new Action(() => lstMensagens.Items.Add($"Cliente desconectado: {nomeCliente}")));
                        break;
                    }
                }
            }
        }

        private void EnviarMensagemTodosClientes(string mensagem, TcpClient remetente)
        {
            //Irá repetir o código abaixo pra cada cliente que tiver conectado com servidor
            foreach (var cliente in clientesConectados)
            {
                //caso o cliente não for igual a remetente
                if (cliente != remetente)
                {
                    try
                    {
                        //vai escrever a mensagem e enviar pra todos os usuários
                        using (var writer = new StreamWriter(cliente.GetStream(), Encoding.UTF8, 999, leaveOpen: true))
                        {
                            writer.WriteLine(mensagem);
                            writer.Flush();
                        }
                    }
                    catch
                    {
                        clientesConectados.Remove(cliente);
                    }
                }
            }
        }


        private void btnLigar_Click(object sender, EventArgs e)
        {
            //código necessário pra ligar o servidor
            int porta = int.Parse(txtPorta.Text);
            servidor = new TcpListener(IPAddress.Any, porta);
            servidor.Start();
            lstMensagens.Items.Add("Servidor iniciado...");

            Task.Run(() => AceitarClientes());
        }
        //código para evitar que digitem letras 
        private void txtPorta_KeyPress(object sender, KeyPressEventArgs e)
        {
            Program.IntNumber(e);
        }
    }


}

