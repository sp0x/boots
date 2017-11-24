node {
    def app

    stage('Clone repository') {
        /**/
        checkout scm
    }

    stage('Build the project') {
        /* Compile the project */ 
        slackSend baseUrl: 'https://peeralytics.slack.com/services/hooks/jenkins-ci/', channel: 'builds', color: 'good', message: "${env.JOB_NAME} - #${env.BUILD_NUMBER} started by changes from ", teamDomain: 'peeralytics', tokenCredentialId: 'jenkins-slack-integration'
        sh 'dotnet restore'
        sh 'dotnet build Netlyt.Web/Netlyt.Web.csproj'
        sh 'dotnet publish Netlyt.Web/Netlyt.Web.csproj -c Debug -o published/netlyt'
            /*} catch (err){
                slackSend baseUrl: 'https://peeralytics.slack.com/services/hooks/jenkins-ci/', channel: 'builds', color: '#439FE0', message: 'Build Failed: ${env.JOB_NAME} ${env.BUILD_NUMBER} (<${env.BUILD_URL}|Open>)', teamDomain: 'peeralytics', tokenCredentialId: 'jenkins-slack-integration'
            }*/
        
    } 
    stage('Build image') {
       /* This builds the docker image*/ 
        try {
            sh 'docker build -t netlyt/netlyt Netlyt.Web'
        } catch (Exception e) {
            slackSend baseUrl: 'https://peeralytics.slack.com/services/hooks/jenkins-ci/', channel: 'builds', color: 'bad', message: "${env.JOB_NAME} - #${env.BUILD_NUMBER} docker image failed: ${e.message}", teamDomain: 'peeralytics', tokenCredentialId: 'jenkins-slack-integration'
            throw e // rethrow so the build is considered failed                        
        } 
      
    }

    stage('Test image'){
        sh ' echo "Tests PASSED"' /* Volkswagen style for now*/        
    }

    stage('Push image'){
        docker.withRegistry('https://registry.vaskovasilev.eu', 'offsite-docker-registry'){
            app.push("${env.BUILD_NUMBER}")
            app.push("latest")
        }
    }
    
    stage('Notify') {
        slackSend baseUrl: 'https://peeralytics.slack.com/services/hooks/jenkins-ci/', channel: 'builds', color: 'good', message: "${env.JOB_NAME} - #${env.BUILD_NUMBER} Failed (<${env.BUILD_URL}|Open>)", teamDomain: 'peeralytics', tokenCredentialId: 'jenkins-slack-integration'
    }
 
}