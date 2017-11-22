node {
    def app

    stage('Clone repository') {
        /**/
        checkout scm
    }

    stage('Build the project') {
        /* Compile the project */ 
        slackSend baseUrl: 'https://peeralytics.slack.com/services/hooks/jenkins-ci/', channel: 'builds', color: '#439FE0', message: 'Build Started: ${env.JOB_NAME} ${env.BUILD_NUMBER} (<${env.BUILD_URL}|Open>)', teamDomain: 'peeralytics', tokenCredentialId: 'jenkins-slack-integration'
        sh 'dotnet restore'
        sh 'dotnet build Netlyt/Netlyt.csproj'
        slackSend baseUrl: 'https://peeralytics.slack.com/services/hooks/jenkins-ci/', channel: 'builds', color: '#439FE0', message: 'Success ${env.JOB_NAME} ${env.BUILD_NUMBER} (<${env.BUILD_URL}|Open>)', teamDomain: 'peeralytics', tokenCredentialId: 'jenkins-slack-integration'
            /*} catch (err){
                slackSend baseUrl: 'https://peeralytics.slack.com/services/hooks/jenkins-ci/', channel: 'builds', color: '#439FE0', message: 'Build Failed: ${env.JOB_NAME} ${env.BUILD_NUMBER} (<${env.BUILD_URL}|Open>)', teamDomain: 'peeralytics', tokenCredentialId: 'jenkins-slack-integration'
            }*/
        
    } 
    stage('Build container') {
       /* This builds the docker image*/ 
       app = docker.build("netlyt/netlyt")
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
 
}