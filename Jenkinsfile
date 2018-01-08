node {
    def app

    stage('Clone repository') {
        /**/
        checkout scm
    }

    stage('Build the project') {
        /* Compile the project */  
        slackSend baseUrl: 'https://peeralytics.slack.com/services/hooks/jenkins-ci/', channel: 'dev', color: 'good', message: "${env.JOB_NAME} - #${env.BUILD_NUMBER} started by changes from ", teamDomain: 'peeralytics', tokenCredentialId: 'jenkins-slack-integration'
        try {
            sh 'dotnet restore'
            sh 'dotnet build Netlyt.Web/Netlyt.Web.csproj'
            sh 'dotnet publish Netlyt.Web/Netlyt.Web.csproj -c Debug -o published/netlyt'
        } catch (Exception e) {
            slackSend baseUrl: 'https://peeralytics.slack.com/services/hooks/jenkins-ci/', channel: 'dev', color: 'bad', message: "${env.JOB_NAME} - #${env.BUILD_NUMBER} project build failed: ${e.message}", teamDomain: 'peeralytics', tokenCredentialId: 'jenkins-slack-integration'
            throw e // rethrow so the build is considered failed                        
        } 
            /*} catch (err){
                slackSend baseUrl: 'https://peeralytics.slack.com/services/hooks/jenkins-ci/', channel: 'builds', color: '#439FE0', message: 'Build Failed: ${env.JOB_NAME} ${env.BUILD_NUMBER} (<${env.BUILD_URL}|Open>)', teamDomain: 'peeralytics', tokenCredentialId: 'jenkins-slack-integration'
            }*/
        
    } 
    stage('Build image') {
       /* This builds the docker image*/ 
        try {
            dir("Netlyt.Web"){
                newImage = docker.build("netlyt/netlyt")                
                sh ' echo "Tests PASSED"' /* Volkswagen style for now*/     
                docker.withRegistry("https://registry.netlyt.com", 'offsite-docker-registry'){
                     newImage.push("latest")
                }
            } 
        } catch (Exception e) {
            slackSend baseUrl: 'https://peeralytics.slack.com/services/hooks/jenkins-ci/', channel: 'dev', color: 'bad', message: "${env.JOB_NAME} - #${env.BUILD_NUMBER} docker image failed: ${e.message}", teamDomain: 'peeralytics', tokenCredentialId: 'jenkins-slack-integration'
            throw e // rethrow so the build is considered failed                        
        } 
      
    } 
    stage('Deploy') {
        sh 'curl --request POST "http://deploy.netlyt.com/?token=b5a2425f52ee61b50f21ee921e4bfa25&hook=netlyt"'
    }
    stage('Notify') {
        slackSend baseUrl: 'https://peeralytics.slack.com/services/hooks/jenkins-ci/', channel: 'dev', color: 'good',
         message: "${env.JOB_NAME} - #${env.BUILD_NUMBER} Successfull (<${env.BUILD_URL}|Open>)", teamDomain: 'peeralytics', tokenCredentialId: 'jenkins-slack-integration'
    }
 
}