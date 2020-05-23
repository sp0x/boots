netlyt:$2y$05$Ka6xc1U6FWxPLBGAaf.XTOcdcVnWsTHeUd/1N2rsx0SrLqzWsI.Dq
admin:$2y$05$DmMrt.3NLLDIIE3RhaH1NOf6e/rlYsv6hV1FFh5xYUy1Qda3elPCC
vasko:$2y$05$/3NbpZHu8LqFa863LysHtuM1qJ4cmJrVq0a.FB3pGSYfe3eFBb1JS
#create with docker run --rm --entrypoint htpasswd registry:2 -Bbn someUsername somePass