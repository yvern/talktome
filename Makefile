net:
ifeq ($(shell docker network ls | grep mmnet),)
	@docker network create mmnet
endif

mattermost: net
ifeq ($(shell docker ps | grep mattermost),)
	@docker run --rm --network=mmnet --name mattermost -d --publish 8065:8065 mattermost/mattermost-preview
endif

ping: mattermost
ifeq ($(shell ls -a | grep tmp),)
	@mkdir .tmp
endif
	@while (! wget -q -O /dev/null localhost:8065 ); do sleep 0.5; done

build: mattermost
ifeq ($(shell docker image ls | grep mmcore-img),)
	@docker build -t mmcore-img .
endif

start: build ping
ifeq ($(shell docker ps | grep mmcore),)
	@docker run --rm -v $(PWD):/app --network=mmnet --name mmcore -d -it mmcore-img
endif

.tmp/admin.json: start .proto/admin.json
	@curl 'http://localhost:8065/api/v4/users' -s -d  '$(shell cat .proto/admin.json)' > .tmp/admin.json

.tmp/group.json: .tmp/admin.json .proto/group.json
	@docker exec mmcore dotnet run new group '$(shell cat .proto/admin.json)' '$(shell cat .proto/group.json)'

.tmp/hook.json: .tmp/group.json
	@docker exec mmcore dotnet run new hook '$(shell cat .proto/admin.json)' '$(shell cat .tmp/group.json)'

chat: .tmp/hook.json
	@curl -X POST -H 'Content-Type: application/json' -d '{"text": "botA:\nhey @botB"}' http://localhost:8065/hooks/$(shell cat .tmp/hook.json)
	@docker exec mmcore dotnet run bot $(shell cat .tmp/hook.json) '$(shell cat .proto/admin.json)' '$(shell cat .tmp/group.json)' botA botB

kill:
	@rm -fr .tmp
ifneq ($(shell docker ps | grep mmcore),)
	@docker stop mmcore
endif
ifneq ($(shell docker ps | grep mattermost),)
	@docker stop mattermost
endif
ifneq ($(shell docker network ls | grep mmnet),)
	@docker network rm mmnet
endif
