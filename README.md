# Matterwagger

## Goal

Chatbot for Mattermost, capable of automatically keeping up to date with Swagger API, written in F#, running on .Net Core 2.2 and deployable to Linux Debian 9.

### Steps

- use Mattermost-preview docker image
- understand it's basic HTTP API
- get acquainted with F# project and dependencies
- use SwaggerProvider to have easy access to Mattermost API
- develop bot conversation logic
- choose release and deployment options

### Shortcommings

- SwaggerProvider dependency problem, incompatible with .Net Core 2.2
  - use beta version
- SwaggerProvider documentation and examples seem to be broken
  - exploratory approach to available members/methods
- long compilation/analysis time for simple programs (on VSCode+Ionide)
  - yaml file has to be parsed on every change, iteration cicles delayed
  - try out new approach: hand made API calls
- direct HTTP calls work as expected, but unsure how to subscribe to mentions/messages
  - found WebSocket API, not sure if FSharp.Data HTTP client is able to work with WebSockets
  - there is a 'posts' api, with a filter to get messages only after certain post ID, which could be used for long-pooling messages
- api for bots not quite clear, and default config on _preview_ image is to disallow bot creation
  - user creation and management is sufficiently simple so that a bot could use a proper user account
