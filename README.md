# Exemplo de Web API em C# com Docker que repete as mensagens enviadas pelo Whatsapp

## Preparação do ambiente

Para o exemplo funcionar é necessário ter uma conta no Whatsapp Business. Siga o [passo a passo](https://developers.facebook.com/docs/whatsapp/cloud-api/get-started) para configurar sua conta e um projeto.

Também é necessário criar um Azure Service App e subir o código do exemplo conforme mostrado [aqui](TODO:Revisar URL)

Este exemplo segue os mesmos princípios explicados nesse [tutorial para AWS](https://developers.facebook.com/docs/whatsapp/cloud-api/guides/set-up-whatsapp-echo-bot). Para saber mais sobre webhooks acesse o [link](https://developers.facebook.com/docs/whatsapp/cloud-api/guides/set-up-webhooks)

No site da Meta para desenvolvedores, abra seus [Apps](https://developers.facebook.com/apps) selecione o app que você criou e na guia "Configuração da API" gere um token "Gerar token de acesso".

Configure um webhook informando a URL da aplicação (por exemplo, https://meuapp.azurewebsites.net/HttpEcho) e informe uma palavra chave (ex.: minhachavesecreta) qualquer que será usada para validação. Ative o campo do tipo **messages** (Deve ficar marcado como Assinado)

Configure as seguintes variáveis de ambiente:

`Whatsapp__VERIFY_TOKEN: minhachavesecreta` (ou outra palavra chave escolhida)

`Whatsapp__ACCESS_TOKEN: EAA...xxx` (token gerado no portal do desenvolvedor)

Existem alguns testes no arquivo Tests.http
