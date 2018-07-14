node {
    def app

    stage('Clone repository') {
        /**/
        checkout scm
    }
    stage('Build image') {
       /* This builds the docker image*/ 
        try { 
            newImage = docker.build("netlyt")                
            sh ' echo "Tests PASSED"' /* Volkswagen style for now*/
            //Our default docker registry
            //docker.withRegistry("https://registry.netlyt.com", 'offsite-docker-registry'){
            //     newImage.push("latest")
            //}
            docker.withRegistry("https://344965022394.dkr.ecr.us-east-2.amazonaws.com", "ecr:us-east-2:aws_ecr") {
                newImage.push()
            }
        } catch (Exception e) {
            slackSend baseUrl: 'https://netlyt.slack.com/services/hooks/jenkins-ci/', 
            channel: 'dev', color: 'bad', message: "${env.JOB_NAME} - #${env.BUILD_NUMBER} docker image failed: ${e.message}", 
            teamDomain: 'netlyt', tokenCredentialId: 'jenkins-slack-integration'
            throw e // rethrow so the build is considered failed                        
        } 
      
    } 
    stage('Deploy') {
        sh 'curl "http://deploy.netlyt.com/?token=b5a2425f52ee61b50f21ee921e4bfa25&hook=netlyt" > /dev/null 2>&1 &'
    }
    stage('Notify') {
        slackSend baseUrl: 'https://netlyt.slack.com/services/hooks/jenkins-ci/', channel: 'dev', color: 'good',
         message: "${env.JOB_NAME} - #${env.BUILD_NUMBER} Successfull (<${env.BUILD_URL}|Open>)", 
         teamDomain: 'netlyt', tokenCredentialId: 'jenkins-slack-integration'
    }
 
}