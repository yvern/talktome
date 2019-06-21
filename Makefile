ddn := $(shell which dotnet)

ifndef (ddn)
ddn := $(shell which dotnet-sdk.dotnethh)
endif


.tmp/start:
	@docker run --rm --name mattermost-preview -d --publish 8065:8065 mattermost/mattermost-preview > .tmp/start

.tmp/ping: .tmp/start
	@while (! wget -q -O /dev/null localhost:8065 ); do sleep 0.5; done
	@touch .tmp/ping

.tmp/admin.json: .tmp/ping .proto/admin.json
	@curl 'http://localhost:8065/api/v4/users' -s -d  '$(shell cat .proto/admin.json)' > .tmp/admin.json

.tmp/group.json: .tmp/admin.json .proto/group.json
	@$(ddn) run new group '$(shell cat .proto/admin.json)' '$(shell cat .proto/group.json)'

.tmp/hook.json: .tmp/group.json
	@$(ddn) run new hook '$(shell cat .proto/admin.json)' '$(shell cat .tmp/group.json)'

chat: .tmp/hook.json
	@curl -X POST -H 'Content-Type: application/json' -d '{"text": "botA:\nhey @botB"}' http://localhost:8065/hooks/$(shell cat .tmp/hook.json)
	@$(ddn) run bot $(shell cat .tmp/hook.json) '$(shell cat .proto/admin.json)' '$(shell cat .tmp/group.json)' botA botB

kill:
	@docker stop mattermost-preview
	@rm -f .tmp/*
