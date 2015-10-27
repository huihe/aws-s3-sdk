
var needle = require('needle');
var endpoint = process.env.ENDPOINT;
var devkey = process.env.DEV_KEY;
var Promise = require('promise');
require('should');
var options = {
    "headers": {
        "x-myobapi-key": devkey,
        "Content-Type": "application/json"
    }
};

var aws = require('aws-sdk');
var s3;

var expected_error_message = '';

function getToken() {
    return new Promise(function (resolve, reject) {
        needle.get(endpoint + '/upload', options, function (err, resp) {
            if (err) {
                return reject('Failed to get a token from Popeye (connectivity) - ' + JSON.stringify(err));
            }
            if (resp.statusCode != 200) {
                return reject('Failed to get a token from Popeye - ' + resp.statusCode);
            }
            aws.config.update({
                accessKeyId: resp.body.accessKeyId,
                secretAccessKey: resp.body.secretAccessKey,
                sessionToken: resp.body.sessionToken
            });
            resolve({ token: resp.body });
        })
    });
}

function uploadFile(packet) {
    return new Promise(function (resolve, reject) {
        s3 = new aws.S3({
            region: packet.token.region
        });

        packet.filename = Math.random();
        packet.contents = Math.random().toString();

        packet.s3object = {
            Bucket: packet.token.bucket,
            Key: packet.token.path + '/' + packet.filename + '.txt',
            ServerSideEncryption: 'AES256',
            Body: new Buffer(packet.contents)
        };

        s3.putObject(packet.s3object, function (err, s3response) {
            if (err) {
                return reject('Failed to write to S3 ' + JSON.stringify(err));
            }
            resolve(packet);
        })
    });
}

function sendMessage(packet) {
    return new Promise(function (resolve, reject) {
        var message = {
            "body": {
                "meta": {
                    "id": "s3-attachment-1",
                    "number": "0012423",
                    "issue_date": "2014-08-10",
                    "due_date": "2014-09-10",
                    "paid_date": "2014-09-10",
                    "status": "Open",
                    "total_amount": "100",
                    "tax_amount": "9",
                    "due_amount": "100",
                    "discount": "0",
                    "currency": "AUD"
                },
                "customer": {
                    "id": "1234567890",
                    "name": "Lucas Eagleton",
                    "government_identifier": "lucas12123123123"
                },
                "company": {
                    "uid": "1234567890",
                    "name": "Penetration Test Company",
                    "email": "lucas.eagleton@myob.com",
                    "country": "AU",
                    "government_identifier": "09098098098"
                },
                "actions": []

            },
            "attachments": [
                {
                    "type": "invoice",
                    "filename": "Sample file.txt",
                    "mime": "application/pdf",
                    "s3_key": packet.s3object.Key,
                    "upload_password": packet.token.uploadPassword
                }
            ]
        };
        needle.post(endpoint + '/invoice', JSON.stringify(message), options, function (err, resp) {
            if (err) {
                return reject('Error sending message' + JSON.stringify(err));
            }
            packet.response = resp.body;
            packet.responseCode = resp.statusCode;
            resolve(packet);
        })

    });
}

function validateHappyResponse(packet) {
    packet.responseCode.should.equal(202);
}

function validateUnhappyResponse(packet) {
    //console.log(packet.response);
    packet.response.message.should.equal(expected_error_message);
    packet.responseCode.should.equal(400);
}

describe('Uploading attachments via S3 first', function () {
    it('should succeed with the happy path', function (done) {
        this.timeout(50000);

        getToken()
            .then(uploadFile)
            .then(sendMessage)
            .then(validateHappyResponse)
            .then(done)
            .catch(function (error) {
                done(new Error(error));
            });
    }, 10000);


    it('should get 400 when i forget to upload', function (done) {
        this.timeout(50000);
        expected_error_message = 'Upload password did not match the path of your attachment';
        getToken()
            //.then(uploadFile)
            .then(function (packet) {
                return new Promise(function (res) {
                    packet.s3object = {
                        Key: 'i forgot this part'
                    };
                    res(packet);
                })
            })
            .then(sendMessage)
            .then(validateUnhappyResponse)
            .then(done)
            .catch(function (error) {
                done(new Error(error));
            });
    }, 10000);

    it('should get 400 when i break the password', function (done) {
        this.timeout(50000);
        expected_error_message = 'Upload password was not valid';

        getToken()
            .then(uploadFile)
            .then(function (packet) {
                return new Promise(function (res) {
                    packet.token.uploadPassword += 'broken'
                    res(packet);
                })
            })
            .then(sendMessage)
            .then(validateUnhappyResponse)
            .then(done)
            .catch(function (error) {
                done(new Error(error));
            });
    }, 10000);

    it('should get 400 when i break the path', function (done) {
        this.timeout(50000);
        expected_error_message = 'Upload password did not match the path of your attachment';
        getToken()
            .then(uploadFile)
            .then(function (packet) {
                return new Promise(function (res) {
                    packet.s3object.Key = 'broken' + packet.s3object.Key
                    res(packet);
                })
            })
            .then(sendMessage)
            .then(validateUnhappyResponse)
            .then(done)
            .catch(function (error) {
                done(new Error(error));
            });
    }, 10000);
}, 100000);
