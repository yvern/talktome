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

- mattermost complete installation seems to be a bit too complex (requires databases and proxy servers, notoriously nontrivial to configure)
  - use the mattermost-preview docker image, which comes vendored with said preconfigured depedencies
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
  - users do not have permission to send posts via api, and couldn't find a way to turn it on (not even making the user admin)
- found (and understood) webhook api
  - with incoming webhooks, bearing the right token, one can send messages via api
    - though preview doesn't seem to support declaring users, so all messages come as from 'admin'
  - with outgoing webhooks, a message starting with a predefined keyword can trigger a post request, so as to notify of an incoming message
    - trouble understanding docker networks, once set up (testing curls from within mattermost container), it became clear mattermost-previview dosen't support outgoing webhooks
  - the right way would be to have a full installation, but setting it up at this point is time prohibitive

### What I should have done

- set up a complete installation builder for mattermost
- write a boot script that generates admin, teams, groups, incoming and outgoing webhooks, enables bot accounts, create such, and save tokens in config file
- run a http server, which receives post requests from mattermost
  - bot dispatch made by route, since multiple endpoints can be set up
  - bots work as simple state machines, where a message triggers a response, which is just a json post request to mattermost
